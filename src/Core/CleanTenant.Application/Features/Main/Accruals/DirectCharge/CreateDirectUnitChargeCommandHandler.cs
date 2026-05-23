using System.Text.Json;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;
using CleanTenant.Application.Features.Main.Accruals.Posting;
using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accruals.DirectCharge;

/// <summary>
/// <see cref="CreateDirectUnitChargeCommand"/> handler — tek BB doğrudan borçlandırma.
/// </summary>
public sealed class CreateDirectUnitChargeCommandHandler
    : IRequestHandler<CreateDirectUnitChargeCommand, Result<AccrualResult>>
{
    private readonly IMainDbContext _db;
    private readonly IAccrualJournalPoster _journalPoster;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateDirectUnitChargeCommandHandler(
        IMainDbContext db,
        IAccrualJournalPoster journalPoster,
        IClock clock,
        ICurrentSessionAccessor session)
    {
        _db = db;
        _journalPoster = journalPoster;
        _clock = clock;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<AccrualResult>> Handle(
        CreateDirectUnitChargeCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0m)
            return Result<AccrualResult>.Failure(Error.Failure("ACR-501", "Tutar sıfırdan büyük olmalı."));

        // Muhasebe dönemi
        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.Id == request.AccountingPeriodId
                                   && p.CompanyId == request.CompanyId
                                   && !p.IsDeleted, cancellationToken);
        if (period is null)
            return Result<AccrualResult>.Failure(Error.NotFound("ACR-502", "Muhasebe dönemi bulunamadı."));

        // BB şirkete ait mi (Unit→Building→Parcel→Land→Company)
        var unitExists = await (
            from u in _db.Units
            join b in _db.Buildings on u.BuildingId equals b.Id
            join p in _db.Parcels on b.ParcelId equals p.Id
            join l in _db.Lands on p.LandId equals l.Id
            where u.Id == request.UnitId && l.CompanyId == request.CompanyId
                && !u.IsDeleted && !b.IsDeleted && !p.IsDeleted && !l.IsDeleted
            select u.Id
        ).AnyAsync(cancellationToken);
        if (!unitExists)
            return Result<AccrualResult>.Failure(Error.NotFound("ACR-503", "Bağımsız bölüm bu sitede bulunamadı."));

        // Hesap kodları geçerli mi
        var codes = await _db.AccountCodes
            .Where(a => (a.Id == request.ReceivableAccountCodeId || a.Id == request.IncomeAccountCodeId)
                     && a.CompanyId == request.CompanyId && !a.IsDeleted)
            .Select(a => new { a.Id, a.IsActive, a.IsDetail })
            .ToListAsync(cancellationToken);
        if (codes.Count != 2 || codes.Any(c => !c.IsActive || !c.IsDetail))
            return Result<AccrualResult>.Failure(
                Error.Failure("ACR-504", "Geçersiz/pasif/özet hesap kodu (yaprak hesap gerekir)."));

        var desc = request.Description.Trim();
        var accrual = new Accrual
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            Source = AccrualSource.DirectCharge,
            AccountingPeriodId = period.Id,
            Year = request.Year,
            Month = request.Month,
            TotalAmount = request.Amount,
            ReceivableAccountCodeId = request.ReceivableAccountCodeId,
            IncomeAccountCodeId = request.IncomeAccountCodeId,
            Description = desc,
            GeneratedAt = _clock.UtcNow,
            GeneratedBy = _session.Current?.UserId,
        };

        var breakdown = JsonSerializer.Serialize(new[]
        {
            new LineBreakdownItem("DIRECT", desc, request.Amount),
        });
        accrual.Details.Add(new AccrualDetail
        {
            TenantId = request.TenantId,
            AccrualId = accrual.Id,
            UnitId = request.UnitId,
            Amount = request.Amount,
            DistributionShare = 1m,
            DueDate = request.DueDate,
            LineBreakdownJson = breakdown,
        });

        _db.Accruals.Add(accrual);

        var postResult = await _journalPoster.PostAsync(accrual, cancellationToken);
        if (postResult.IsFailure)
            return Result<AccrualResult>.Failure(postResult.FirstError);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<AccrualResult>.Success(
            new AccrualResult(accrual.Id, accrual.TotalAmount, accrual.Details.Count));
    }
}
