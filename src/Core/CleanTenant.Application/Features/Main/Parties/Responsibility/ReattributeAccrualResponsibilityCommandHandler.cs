using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.Domain.Tenant.Parties;
using CleanTenant.Domain.Tenant.Parties.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Parties.Responsibility;

/// <summary><see cref="ReattributeAccrualResponsibilityCommand"/> handler (GUARD'lı).</summary>
public sealed class ReattributeAccrualResponsibilityCommandHandler
    : IRequestHandler<ReattributeAccrualResponsibilityCommand, Result<ReattributeResult>>
{
    private readonly IMainDbContext _db;
    private readonly IResponsibilityResolver _resolver;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReattributeAccrualResponsibilityCommandHandler(IMainDbContext db, IResponsibilityResolver resolver)
    {
        _db = db;
        _resolver = resolver;
    }

    /// <inheritdoc />
    public async Task<Result<ReattributeResult>> Handle(
        ReattributeAccrualResponsibilityCommand request, CancellationToken cancellationToken)
    {
        // Bu BB'nin bütçe kaynaklı tahakkuk detayları (period + mode ile)
        var details = await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where d.UnitId == request.UnitId
                && a.CompanyId == request.CompanyId
                && a.Source == AccrualSource.Budget
                && !d.IsDeleted && !a.IsDeleted
            select new { Detail = d, a.Year, a.Month, a.ResponsibilityMode }
        ).ToListAsync(cancellationToken);

        if (details.Count == 0)
            return Result<ReattributeResult>.Success(new ReattributeResult(0, 0));

        var detailIds = details.Select(x => x.Detail.Id).ToList();

        // GUARD kaynak 1: tahsilatı olan detaylar (ödeme dokunmuş)
        var paidDetailIds = (await _db.CollectionAllocations
            .Where(al => detailIds.Contains(al.AccrualDetailId) && !al.IsDeleted)
            .Select(al => al.AccrualDetailId)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet();

        // GUARD kaynak 2: bu BB'de gecikme faizi olan dönemler (year, month)
        var lateFeePeriods = (await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where d.UnitId == request.UnitId
                && a.CompanyId == request.CompanyId
                && a.Source == AccrualSource.LateFee
                && !d.IsDeleted && !a.IsDeleted
            select new { a.Year, a.Month }
        ).Distinct().ToListAsync(cancellationToken))
            .Select(x => (x.Year, x.Month))
            .ToHashSet();

        var recomputed = 0;
        var skipped = 0;

        foreach (var x in details)
        {
            // GUARD: ödeme veya gecikme dokunmuş dönem → sessiz düzeltme yapma
            if (paidDetailIds.Contains(x.Detail.Id) || lateFeePeriods.Contains((x.Year, x.Month)))
            {
                skipped++;
                continue;
            }

            var mode = x.ResponsibilityMode ?? ResponsibilityMode.TenantThenOwner;
            var prorated = await _resolver.ProrateBatchAsync(
                [new UnitAccrualInput(request.UnitId, x.Detail.Amount)], x.Year, x.Month, mode, cancellationToken);
            if (!prorated.TryGetValue(request.UnitId, out var r))
            {
                skipped++;
                continue;
            }

            // Eski parçaları soft-delete
            var oldSplits = await _db.AccrualResponsibilitySplits
                .Where(s => s.AccrualDetailId == x.Detail.Id && !s.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var os in oldSplits) os.IsDeleted = true;

            // Yeniden ata
            x.Detail.PrimaryResponsiblePartyId = r.PrimaryPartyId;
            x.Detail.ResponsibleResolvedNote = r.Note;
            foreach (var sp in r.Splits)
                _db.AccrualResponsibilitySplits.Add(new AccrualResponsibilitySplit
                {
                    TenantId = x.Detail.TenantId,
                    AccrualDetailId = x.Detail.Id,
                    PartyId = sp.PartyId,
                    Kind = sp.Kind,
                    FromDate = sp.FromDate,
                    ToDate = sp.ToDate,
                    DayCount = sp.DayCount,
                    Amount = sp.Amount,
                });
            recomputed++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<ReattributeResult>.Success(new ReattributeResult(recomputed, skipped));
    }
}
