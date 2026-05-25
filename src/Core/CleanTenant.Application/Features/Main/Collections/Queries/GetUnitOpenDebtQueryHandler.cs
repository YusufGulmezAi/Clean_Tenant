using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Collections.Queries;

/// <summary><see cref="GetUnitOpenDebtQuery"/> handler — açık tahakkuk detayları, TBK m.101 sırasıyla.</summary>
public sealed class GetUnitOpenDebtQueryHandler
    : IRequestHandler<GetUnitOpenDebtQuery, Result<UnitOpenDebt>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUnitOpenDebtQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<UnitOpenDebt>> Handle(
        GetUnitOpenDebtQuery request, CancellationToken cancellationToken)
    {
        // Yalnız ödenebilir (Correction olmayan) detaylar; düzeltmeler aşağıda netlenir.
        var details = await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where d.UnitId == request.UnitId && a.CompanyId == request.CompanyId
                && !d.IsDeleted && !a.IsDeleted && a.Source != AccrualSource.Correction
            select new
            {
                d.Id,
                d.Amount,
                d.DueDate,
                a.Source,
                a.Year,
                a.Month,
                a.Description
            }).ToListAsync(cancellationToken);

        var detailIds = details.Select(x => x.Id).ToList();
        var allocatedMap = (await _db.CollectionAllocations
            .Where(al => detailIds.Contains(al.AccrualDetailId) && !al.IsDeleted)
            .GroupBy(al => al.AccrualDetailId)
            .Select(g => new { DetailId = g.Key, Sum = g.Sum(x => x.AllocatedAmount) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DetailId, x => x.Sum);

        // Ters kayıt netlemesi: her orijinal detaya bağlı Correction tutarları (pozitif toplam)
        var correctionMap = (await _db.AccrualDetails
            .Where(d => d.CorrectedAccrualDetailId != null
                     && detailIds.Contains(d.CorrectedAccrualDetailId.Value) && !d.IsDeleted)
            .GroupBy(d => d.CorrectedAccrualDetailId!.Value)
            .Select(g => new { DetailId = g.Key, Sum = g.Sum(x => -x.Amount) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DetailId, x => x.Sum);

        var lines = details
            .Select(d => new
            {
                d.Id,
                d.DueDate,
                d.Source,
                d.Year,
                d.Month,
                d.Description,
                Remaining = d.Amount - allocatedMap.GetValueOrDefault(d.Id, 0m)
                            - correctionMap.GetValueOrDefault(d.Id, 0m),
            })
            .Where(d => d.Remaining > 0m)
            // TBK m.101: en eski vade içinde önce gecikme faizi, sonra anapara
            .OrderBy(d => d.DueDate)
            .ThenByDescending(d => d.Source == AccrualSource.LateFee)
            .ThenBy(d => d.Id)
            .Select(d => new OpenDebtLine(d.Id, d.DueDate, d.Source, d.Year, d.Month, d.Description, d.Remaining))
            .ToList();

        var total = lines.Sum(l => l.OpenAmount);
        return Result<UnitOpenDebt>.Success(new UnitOpenDebt(total, lines));
    }
}
