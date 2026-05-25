using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Accruals.Posting;
using CleanTenant.Application.Features.Main.LateFees.Calculation;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.LateFees.GenerateLateFeeCharges;

/// <summary>
/// <see cref="GenerateLateFeeChargesCommand"/> handler — vadesi geçmiş açık
/// borçlara gecikme faizi tahakkuğu + otomatik yevmiye. Bkz. 06-LATEFEE-DESIGN.md.
/// </summary>
public sealed class GenerateLateFeeChargesCommandHandler
    : IRequestHandler<GenerateLateFeeChargesCommand, Result<LateFeeChargeResult>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;
    private readonly ILateFeeCalculator _calculator;
    private readonly ILateFeePolicyResolver _resolver;
    private readonly IAccrualJournalPoster _journalPoster;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GenerateLateFeeChargesCommandHandler(
        IMainDbContext db,
        IClock clock,
        ICurrentSessionAccessor session,
        ILateFeeCalculator calculator,
        ILateFeePolicyResolver resolver,
        IAccrualJournalPoster journalPoster)
    {
        _db = db;
        _clock = clock;
        _session = session;
        _calculator = calculator;
        _resolver = resolver;
        _journalPoster = journalPoster;
    }

    /// <inheritdoc />
    public async Task<Result<LateFeeChargeResult>> Handle(
        GenerateLateFeeChargesCommand request, CancellationToken cancellationToken)
    {
        // ── 1. Muhasebe dönemi (AsOfDate ayı) ────────────────────────────────────
        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.CompanyId == request.CompanyId
                                   && p.Year == request.AsOfDate.Year
                                   && p.Month == request.AsOfDate.Month
                                   && !p.IsDeleted, cancellationToken);
        if (period is null)
            return Result<LateFeeChargeResult>.Failure(
                Error.NotFound("LF-001", "AsOfDate için muhasebe dönemi bulunamadı."));
        if (period.Status != PeriodStatus.Open)
            return Result<LateFeeChargeResult>.Failure(
                Error.Failure("LF-002", "Kapalı döneme gecikme faizi işlenemez."));

        // ── 2. Açık + vadesi geçmiş anapara detayları (LateFee + Correction hariç) ─
        var principal = await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where a.CompanyId == request.CompanyId
                && a.Source != AccrualSource.LateFee
                && a.Source != AccrualSource.Correction
                && !d.IsDeleted && !a.IsDeleted
            select new { d.Id, d.UnitId, d.Amount, d.DueDate, a.BudgetId, a.ReceivableAccountCodeId }
        ).ToListAsync(cancellationToken);

        if (principal.Count == 0)
            return Result<LateFeeChargeResult>.Success(new LateFeeChargeResult(0, 0, 0m));

        // Tahsis düşümü → kalan anapara
        var principalIds = principal.Select(x => x.Id).ToList();
        var allocByDetail = (await _db.CollectionAllocations
            .Where(al => principalIds.Contains(al.AccrualDetailId) && !al.IsDeleted)
            .GroupBy(al => al.AccrualDetailId)
            .Select(g => new { DetailId = g.Key, Sum = g.Sum(x => x.AllocatedAmount) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DetailId, x => x.Sum);

        // Ters kayıt netlemesi: anaparaya bağlı düzeltmeler gecikme tabanından düşülür
        // → düzeltilmiş anaparaya gecikme işlenir (fazla faiz önlenir).
        var principalCorrections = (await _db.AccrualDetails
            .Where(d => d.CorrectedAccrualDetailId != null
                     && principalIds.Contains(d.CorrectedAccrualDetailId.Value) && !d.IsDeleted)
            .GroupBy(d => d.CorrectedAccrualDetailId!.Value)
            .Select(g => new { DetailId = g.Key, Sum = g.Sum(x => -x.Amount) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DetailId, x => x.Sum);

        // ── 3. Halihazırda işlenmiş gecikme (BB bazında) ─────────────────────────
        var alreadyByUnit = (await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where a.CompanyId == request.CompanyId
                && a.Source == AccrualSource.LateFee
                && !d.IsDeleted && !a.IsDeleted
            group d by d.UnitId into g
            select new { UnitId = g.Key, Sum = g.Sum(x => x.Amount) }
        ).ToListAsync(cancellationToken))
            .ToDictionary(x => x.UnitId, x => x.Sum);

        // ── 4. Aktif politikalar (in-memory çözümleme) ───────────────────────────
        var policies = await _db.LateFeePolicies
            .Where(p => p.CompanyId == request.CompanyId && p.IsActive && !p.IsDeleted)
            .ToListAsync(cancellationToken);
        if (policies.Count == 0)
            return Result<LateFeeChargeResult>.Failure(
                Error.Failure("LF-003", "Tanımlı gecikme faizi politikası yok."));

        // ── 5. Açık + vadesi geçmiş kalemler ─────────────────────────────────────
        var overdue = principal
            .Select(x => new
            {
                x.UnitId,
                x.DueDate,
                x.BudgetId,
                x.ReceivableAccountCodeId,
                Remaining = x.Amount - allocByDetail.GetValueOrDefault(x.Id, 0m)
                            - principalCorrections.GetValueOrDefault(x.Id, 0m),
            })
            .Where(x => x.Remaining > 0m && x.DueDate < request.AsOfDate)
            .ToList();

        // (receivable, income) çiftine göre gruplanmış BB gecikmeleri
        var groups = new Dictionary<(Guid Recv, Guid Income), List<(Guid UnitId, decimal Amount, DateOnly DueDate)>>();
        foreach (var unitGroup in overdue.GroupBy(x => x.UnitId))
        {
            var debts = unitGroup.OrderBy(d => d.DueDate).ToList();
            var policy = _resolver.Resolve(policies, debts[0].BudgetId);
            if (policy is null)
                continue; // BB için politika yok → atla

            // En eski (alacak hesabı dolu) borcun hesabı kullanılır (MVP sadeleştirmesi)
            var recvDebt = debts.FirstOrDefault(d => d.ReceivableAccountCodeId is not null);
            if (recvDebt is null || recvDebt.ReceivableAccountCodeId is not { } recvId)
                continue;

            var computed = 0m;
            foreach (var d in debts)
                computed += _calculator.ComputeForDebt(
                    d.Remaining, d.DueDate, policy.GraceDays, policy.MonthlyRatePercent, request.AsOfDate);
            computed = Math.Round(computed, 2, MidpointRounding.AwayFromZero);

            var already = alreadyByUnit.GetValueOrDefault(unitGroup.Key, 0m);
            var delta = computed - already;
            if (delta <= 0m)
                continue;

            var key = (recvId, policy.IncomeAccountCodeId);
            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }
            list.Add((unitGroup.Key, delta, debts[0].DueDate));
        }

        if (groups.Count == 0)
            return Result<LateFeeChargeResult>.Success(new LateFeeChargeResult(0, 0, 0m));

        // ── 6. Grup başına LateFee Accrual + Posted yevmiye ──────────────────────
        var now = _clock.UtcNow;
        var userId = _session.Current?.UserId;
        var chargedUnits = 0;
        var accrualCount = 0;
        var totalAll = 0m;

        foreach (var (key, list) in groups)
        {
            var total = list.Sum(x => x.Amount);
            var accrual = new Accrual
            {
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                Source = AccrualSource.LateFee,
                AccountingPeriodId = period.Id,
                Year = request.AsOfDate.Year,
                Month = request.AsOfDate.Month,
                TotalAmount = total,
                ReceivableAccountCodeId = key.Recv,
                IncomeAccountCodeId = key.Income,
                Description = $"Gecikme faizi — {request.AsOfDate:yyyy-MM-dd}",
                GeneratedAt = now,
                GeneratedBy = userId,
            };
            foreach (var (unitId, amount, dueDate) in list)
            {
                accrual.Details.Add(new AccrualDetail
                {
                    TenantId = request.TenantId,
                    AccrualId = accrual.Id,
                    UnitId = unitId,
                    Amount = amount,
                    DistributionShare = 1m,
                    DueDate = dueDate,
                });
            }
            _db.Accruals.Add(accrual);

            var postResult = await _journalPoster.PostAsync(accrual, cancellationToken);
            if (postResult.IsFailure)
                return Result<LateFeeChargeResult>.Failure(postResult.FirstError);

            chargedUnits += list.Count;
            totalAll += total;
            accrualCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<LateFeeChargeResult>.Success(
            new LateFeeChargeResult(chargedUnits, accrualCount, totalAll));
    }
}
