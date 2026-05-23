using System.Text.Json;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Accruals.Distribution;
using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;

/// <summary>
/// <see cref="GenerateBudgetAccrualCommand"/> handler — tahakkuk üretim motoru.
/// </summary>
public sealed class GenerateBudgetAccrualCommandHandler
    : IRequestHandler<GenerateBudgetAccrualCommand, Result<AccrualResult>>
{
    private readonly IMainDbContext _db;
    private readonly IDistributionService _distribution;
    private readonly IAccountCodeAllocator _allocator;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GenerateBudgetAccrualCommandHandler(
        IMainDbContext db,
        IDistributionService distribution,
        IAccountCodeAllocator allocator,
        IClock clock,
        ICurrentSessionAccessor session)
    {
        _db = db;
        _distribution = distribution;
        _allocator = allocator;
        _clock = clock;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<AccrualResult>> Handle(
        GenerateBudgetAccrualCommand request, CancellationToken cancellationToken)
    {
        // ── 1. Bütçe ───────────────────────────────────────────────────────────
        var budget = await _db.Budgets
            .FirstOrDefaultAsync(b => b.Id == request.BudgetId
                                   && b.CompanyId == request.CompanyId
                                   && !b.IsDeleted, cancellationToken);
        if (budget is null)
            return Result<AccrualResult>.Failure(Error.NotFound("ACR-001", "Bütçe bulunamadı."));
        if (budget.Status != BudgetStatus.Published)
            return Result<AccrualResult>.Failure(Error.Failure("ACR-002", "Yalnız yayınlı bütçe için tahakkuk üretilir."));

        // ── 2. Dönem bütçe penceresinde mi ───────────────────────────────────────
        var periodIdx = request.Year * 12 + request.Month;
        var budgetStartIdx = budget.PeriodStartYear * 12 + budget.PeriodStartMonth;
        var budgetEndIdx = budget.PeriodEndYear * 12 + budget.PeriodEndMonth;
        if (periodIdx < budgetStartIdx || periodIdx > budgetEndIdx)
            return Result<AccrualResult>.Failure(
                Error.Failure("ACR-003", "Dönem bütçe geçerlilik aralığı dışında."));

        // ── 3. Muhasebe dönemi ────────────────────────────────────────────────────
        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.FiscalYearId == budget.FiscalYearId
                                   && p.Year == request.Year
                                   && p.Month == request.Month
                                   && !p.IsDeleted, cancellationToken);
        if (period is null)
            return Result<AccrualResult>.Failure(Error.NotFound("ACR-004", "Muhasebe dönemi bulunamadı."));

        // ── 4. İdempotency ────────────────────────────────────────────────────────
        var existing = await _db.Accruals
            .FirstOrDefaultAsync(a => a.BudgetId == request.BudgetId
                                   && a.AccountingPeriodId == period.Id
                                   && a.Source == AccrualSource.Budget
                                   && !a.IsDeleted, cancellationToken);
        if (existing is not null)
        {
            // FAZ 7'de tahsilat kontrolü eklenecek; şimdilik yalnız force.
            if (!request.Force)
                return Result<AccrualResult>.Failure(
                    Error.Conflict("ACR-005", "Bu dönem için tahakkuk zaten üretilmiş. Yeniden üretmek için onay gerekir."));

            existing.IsDeleted = true;
            var oldDetails = await _db.AccrualDetails
                .Where(d => d.AccrualId == existing.Id && !d.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var d in oldDetails) d.IsDeleted = true;
        }

        // ── 5. Aktif bütçe versiyonu ──────────────────────────────────────────────
        var periodFirstDay = new DateOnly(request.Year, request.Month, 1);
        var periodLastDay = periodFirstDay.AddMonths(1).AddDays(-1);

        var version = await _db.BudgetVersions
            .Where(v => v.BudgetId == budget.Id
                     && v.PublishedAt != null
                     && !v.IsDeleted
                     && v.ValidFrom != null && v.ValidFrom <= periodLastDay
                     && (v.ValidTo == null || v.ValidTo >= periodFirstDay))
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (version is null)
            return Result<AccrualResult>.Failure(
                Error.Failure("ACR-006", "Dönem için geçerli yayınlı bütçe versiyonu yok."));

        // ── 6. Kalem versiyonları + taksitler ─────────────────────────────────────
        var lineVersions = await _db.BudgetLineVersions
            .Where(lv => lv.BudgetVersionId == version.Id && !lv.IsDeleted)
            .ToListAsync(cancellationToken);
        if (lineVersions.Count == 0)
            return Result<AccrualResult>.Failure(
                Error.Failure("ACR-007", "Bütçe versiyonunda kalem yok."));

        var lineIds = lineVersions.Select(lv => lv.BudgetLineId).ToList();
        var lines = await _db.BudgetLines
            .Where(l => lineIds.Contains(l.Id) && !l.IsDeleted)
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var versionLineVersionIds = lineVersions.Select(lv => lv.Id).ToList();
        var installments = await _db.BudgetLineInstallments
            .Where(i => versionLineVersionIds.Contains(i.BudgetLineVersionId)
                     && i.Year == request.Year && i.Month == request.Month
                     && !i.IsDeleted)
            .ToListAsync(cancellationToken);
        var installmentByLineVersion = installments.ToDictionary(i => i.BudgetLineVersionId, i => i.Amount);

        // ── 7. Şirketin BB'leri (Unit→Building→Parcel→Land→Company) ──────────────
        var units = await (
            from u in _db.Units
            join b in _db.Buildings on u.BuildingId equals b.Id
            join p in _db.Parcels on b.ParcelId equals p.Id
            join l in _db.Lands on p.LandId equals l.Id
            where l.CompanyId == request.CompanyId
                && !u.IsDeleted && !b.IsDeleted && !p.IsDeleted && !l.IsDeleted
            select new { u.Id, u.GrossSquareMeters }
        ).ToListAsync(cancellationToken);
        if (units.Count == 0)
            return Result<AccrualResult>.Failure(
                Error.Failure("ACR-008", "Sitede bağımsız bölüm bulunamadı."));

        var allUnitIds = units.Select(u => u.Id).ToHashSet();

        // ── 8. Katılım üyelikleri + muafiyetler (dönemde aktif) ──────────────────
        var groupIds = lineVersions
            .Where(lv => lv.ParticipationGroupId.HasValue)
            .Select(lv => lv.ParticipationGroupId!.Value)
            .Distinct()
            .ToList();

        var memberships = await _db.UnitParticipationGroups
            .Where(m => groupIds.Contains(m.ParticipationGroupId) && !m.IsDeleted
                     && m.ValidFrom <= periodLastDay
                     && (m.ValidTo == null || m.ValidTo >= periodFirstDay))
            .Select(m => new { m.ParticipationGroupId, m.UnitId })
            .ToListAsync(cancellationToken);
        var membersByGroup = memberships
            .GroupBy(m => m.ParticipationGroupId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UnitId).ToHashSet());

        var exemptions = await _db.ExemptionRules
            .Where(e => e.CompanyId == request.CompanyId && !e.IsDeleted
                     && e.ValidFrom <= periodLastDay
                     && (e.ValidTo == null || e.ValidTo >= periodFirstDay))
            .Select(e => new { e.UnitId, e.BudgetLineId })
            .ToListAsync(cancellationToken);
        var exemptByLine = exemptions
            .GroupBy(e => e.BudgetLineId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UnitId).ToHashSet());

        // ── 9. Dağıtım ─────────────────────────────────────────────────────────────
        var unitTotals = new Dictionary<Guid, decimal>();
        var unitBreakdown = new Dictionary<Guid, List<LineBreakdownItem>>();
        var minDueDay = 28;

        foreach (var lv in lineVersions)
        {
            var amount = ComputeMonthlyAmount(lv, budget, request.Month, installmentByLineVersion);
            if (amount <= 0m) continue;

            // Katılımcı BB'ler
            IEnumerable<Guid> candidateIds = lv.ParticipationGroupId.HasValue
                ? membersByGroup.GetValueOrDefault(lv.ParticipationGroupId.Value, []).Where(allUnitIds.Contains)
                : allUnitIds;

            // Muafiyetleri çıkar
            var exemptSet = exemptByLine.GetValueOrDefault(lv.BudgetLineId, []);
            var targetIds = candidateIds.Where(id => !exemptSet.Contains(id)).ToHashSet();
            if (targetIds.Count == 0) continue;

            var targetUnits = units.Where(u => targetIds.Contains(u.Id))
                .Select(u => new DistributionUnit(u.Id, u.GrossSquareMeters))
                .ToList();

            var shares = _distribution.Distribute(lv.DistributionModel, amount, targetUnits);

            var line = lines.GetValueOrDefault(lv.BudgetLineId);
            var lineCode = line?.Code ?? "?";
            var lineName = line?.Name ?? "?";
            if (lv.DueDayOfMonth < minDueDay) minDueDay = lv.DueDayOfMonth;

            foreach (var s in shares)
            {
                if (s.Amount <= 0m) continue;
                unitTotals[s.UnitId] = unitTotals.GetValueOrDefault(s.UnitId) + s.Amount;
                if (!unitBreakdown.TryGetValue(s.UnitId, out var list))
                {
                    list = [];
                    unitBreakdown[s.UnitId] = list;
                }
                list.Add(new LineBreakdownItem(lineCode, lineName, s.Amount));
            }
        }

        if (unitTotals.Count == 0)
            return Result<AccrualResult>.Failure(
                Error.Failure("ACR-009", "Bu dönemde tahakkuk edilecek tutar yok (ödeme planı/katılım/muafiyet sonrası boş)."));

        // ── 10. Vade tarihi (ertesi ay, minimum DueDay) ──────────────────────────
        var dueMonth = periodFirstDay.AddMonths(1);
        var dueDay = Math.Min(minDueDay, DateTime.DaysInMonth(dueMonth.Year, dueMonth.Month));
        var dueDate = new DateOnly(dueMonth.Year, dueMonth.Month, dueDay);

        // ── 11. Hesap kodu tahsisi (ilk tahakkukta) ──────────────────────────────
        if (budget.ReceivableAccountCodeId is null || budget.IncomeAccountCodeId is null)
        {
            var pair = await _allocator.AllocateBudgetAccountCodesAsync(
                budget.TenantId, budget.CompanyId, budget.Type, budget.Title, cancellationToken);
            budget.ReceivableAccountCodeId = pair.ReceivableAccountCodeId;
            budget.IncomeAccountCodeId = pair.IncomeAccountCodeId;
        }

        // ── 12. Accrual + Details ─────────────────────────────────────────────────
        var total = unitTotals.Values.Sum();
        var accrual = new Accrual
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            Source = AccrualSource.Budget,
            BudgetId = budget.Id,
            BudgetVersionId = version.Id,
            AccountingPeriodId = period.Id,
            Year = request.Year,
            Month = request.Month,
            TotalAmount = total,
            ReceivableAccountCodeId = budget.ReceivableAccountCodeId,
            IncomeAccountCodeId = budget.IncomeAccountCodeId,
            JournalEntryId = null, // Slice 6.5b'de yevmiye fişi postingi
            Description = $"{budget.Title} — {request.Month:00}/{request.Year} Tahakkuk",
            GeneratedAt = _clock.UtcNow,
            GeneratedBy = _session.Current?.UserId,
        };

        foreach (var (unitId, amount) in unitTotals)
        {
            var breakdown = unitBreakdown[unitId];
            var shareRatio = total == 0m ? 0m : amount / total;
            accrual.Details.Add(new AccrualDetail
            {
                TenantId = request.TenantId,
                AccrualId = accrual.Id,
                UnitId = unitId,
                Amount = amount,
                DistributionShare = shareRatio,
                DueDate = dueDate,
                LineBreakdownJson = JsonSerializer.Serialize(breakdown),
            });
        }

        _db.Accruals.Add(accrual);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<AccrualResult>.Success(
            new AccrualResult(accrual.Id, total, accrual.Details.Count));
    }

    /// <summary>Bir kalem versiyonunun belirtilen ay için tahakkuk tutarını hesaplar.</summary>
    private static decimal ComputeMonthlyAmount(
        BudgetLineVersion lv, Budget budget, int month,
        IReadOnlyDictionary<Guid, decimal> installmentByLineVersion)
    {
        switch (lv.PaymentSchedule)
        {
            case PaymentSchedule.MonthlyEqual:
                var monthCount = (budget.PeriodEndYear * 12 + budget.PeriodEndMonth)
                               - (budget.PeriodStartYear * 12 + budget.PeriodStartMonth) + 1;
                if (monthCount <= 0) monthCount = 1;
                return Math.Round(lv.PlannedAmount / monthCount, 2, MidpointRounding.AwayFromZero);

            case PaymentSchedule.AnnualLumpSum:
                // Tetik ayı = bütçe başlangıç ayı (MVP varsayımı)
                return month == budget.PeriodStartMonth ? lv.PlannedAmount : 0m;

            case PaymentSchedule.Installment:
                return installmentByLineVersion.GetValueOrDefault(lv.Id, 0m);

            case PaymentSchedule.InvoiceBased:
            default:
                return 0m; // otomatik üretilmez
        }
    }
}

/// <summary>AccrualDetail.LineBreakdownJson için kalem-bazlı kırılım öğesi.</summary>
public sealed record LineBreakdownItem(string LineCode, string LineName, decimal Amount);
