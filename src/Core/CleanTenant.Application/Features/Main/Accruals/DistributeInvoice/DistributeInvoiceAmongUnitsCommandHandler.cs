using System.Text.Json;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Accruals.Distribution;
using CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;
using CleanTenant.Application.Features.Main.Accruals.Posting;
using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accruals.DistributeInvoice;

/// <summary>
/// <see cref="DistributeInvoiceAmongUnitsCommand"/> handler — fatura-bazlı tahakkuk.
/// </summary>
public sealed class DistributeInvoiceAmongUnitsCommandHandler
    : IRequestHandler<DistributeInvoiceAmongUnitsCommand, Result<AccrualResult>>
{
    private readonly IMainDbContext _db;
    private readonly IDistributionService _distribution;
    private readonly IAccrualJournalPoster _journalPoster;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DistributeInvoiceAmongUnitsCommandHandler(
        IMainDbContext db,
        IDistributionService distribution,
        IAccrualJournalPoster journalPoster,
        IClock clock,
        ICurrentSessionAccessor session)
    {
        _db = db;
        _distribution = distribution;
        _journalPoster = journalPoster;
        _clock = clock;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<AccrualResult>> Handle(
        DistributeInvoiceAmongUnitsCommand request, CancellationToken cancellationToken)
    {
        if (request.TotalAmount <= 0m)
            return Result<AccrualResult>.Failure(Error.Failure("ACR-401", "Tutar sıfırdan büyük olmalı."));

        // Muhasebe dönemi
        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.Id == request.AccountingPeriodId
                                   && p.CompanyId == request.CompanyId
                                   && !p.IsDeleted, cancellationToken);
        if (period is null)
            return Result<AccrualResult>.Failure(Error.NotFound("ACR-402", "Muhasebe dönemi bulunamadı."));

        // Hesap kodları geçerli mi (şirkete ait, aktif, yaprak)
        var codes = await _db.AccountCodes
            .Where(a => (a.Id == request.ReceivableAccountCodeId || a.Id == request.IncomeAccountCodeId)
                     && a.CompanyId == request.CompanyId && !a.IsDeleted)
            .Select(a => new { a.Id, a.IsActive, a.IsDetail })
            .ToListAsync(cancellationToken);
        if (codes.Count != 2 || codes.Any(c => !c.IsActive || !c.IsDetail))
            return Result<AccrualResult>.Failure(
                Error.Failure("ACR-403", "Geçersiz/pasif/özet hesap kodu (yaprak hesap gerekir)."));

        // İdempotency: aynı fatura
        if (request.InvoiceId is { } invId)
        {
            var dup = await _db.Accruals.AnyAsync(
                a => a.InvoiceId == invId && a.Source == AccrualSource.Invoice && !a.IsDeleted,
                cancellationToken);
            if (dup)
                return Result<AccrualResult>.Failure(
                    Error.Conflict("ACR-405", "Bu fatura için tahakkuk zaten üretilmiş."));
        }

        var periodFirstDay = new DateOnly(request.Year, request.Month, 1);
        var periodLastDay = periodFirstDay.AddMonths(1).AddDays(-1);

        // Şirketin BB'leri
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
            return Result<AccrualResult>.Failure(Error.Failure("ACR-406", "Sitede bağımsız bölüm yok."));

        // Katılım grubu filtresi (verildiyse dönemde aktif üyeler)
        var targetUnits = units;
        if (request.ParticipationGroupId is { } pgId)
        {
            var memberIds = await _db.UnitParticipationGroups
                .Where(m => m.ParticipationGroupId == pgId && !m.IsDeleted
                         && m.ValidFrom <= periodLastDay
                         && (m.ValidTo == null || m.ValidTo >= periodFirstDay))
                .Select(m => m.UnitId)
                .ToListAsync(cancellationToken);
            var memberSet = memberIds.ToHashSet();
            targetUnits = units.Where(u => memberSet.Contains(u.Id)).ToList();
            if (targetUnits.Count == 0)
                return Result<AccrualResult>.Failure(
                    Error.Failure("ACR-407", "Katılım grubunda dönemde aktif BB yok."));
        }

        // Dağıt
        var shares = _distribution.Distribute(
            request.DistributionModel,
            request.TotalAmount,
            targetUnits.Select(u => new DistributionUnit(u.Id, u.GrossSquareMeters)).ToList());

        var desc = request.Description.Trim();
        var accrual = new Accrual
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            Source = AccrualSource.Invoice,
            InvoiceId = request.InvoiceId,
            AccountingPeriodId = period.Id,
            Year = request.Year,
            Month = request.Month,
            TotalAmount = request.TotalAmount,
            ReceivableAccountCodeId = request.ReceivableAccountCodeId,
            IncomeAccountCodeId = request.IncomeAccountCodeId,
            Description = desc,
            GeneratedAt = _clock.UtcNow,
            GeneratedBy = _session.Current?.UserId,
        };

        foreach (var s in shares)
        {
            if (s.Amount <= 0m) continue;
            var breakdown = JsonSerializer.Serialize(new[]
            {
                new LineBreakdownItem("INV", desc, s.Amount),
            });
            accrual.Details.Add(new AccrualDetail
            {
                TenantId = request.TenantId,
                AccrualId = accrual.Id,
                UnitId = s.UnitId,
                Amount = s.Amount,
                DistributionShare = s.Share,
                DueDate = request.DueDate,
                LineBreakdownJson = breakdown,
            });
        }

        if (accrual.Details.Count == 0)
            return Result<AccrualResult>.Failure(Error.Failure("ACR-408", "Dağıtım sonucu boş."));

        _db.Accruals.Add(accrual);

        var postResult = await _journalPoster.PostAsync(accrual, cancellationToken);
        if (postResult.IsFailure)
            return Result<AccrualResult>.Failure(postResult.FirstError);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<AccrualResult>.Success(
            new AccrualResult(accrual.Id, accrual.TotalAmount, accrual.Details.Count));
    }
}
