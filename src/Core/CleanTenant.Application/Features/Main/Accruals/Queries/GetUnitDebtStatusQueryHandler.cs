using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accruals.Queries;

/// <summary><see cref="GetUnitDebtStatusQuery"/> handler.</summary>
public sealed class GetUnitDebtStatusQueryHandler
    : IRequestHandler<GetUnitDebtStatusQuery, Result<UnitDebtStatus>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUnitDebtStatusQueryHandler(IMainDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<UnitDebtStatus>> Handle(
        GetUnitDebtStatusQuery request, CancellationToken cancellationToken)
    {
        var rows = await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where d.UnitId == request.UnitId
                && a.CompanyId == request.CompanyId
                && !d.IsDeleted && !a.IsDeleted
            select new { d.Id, d.Amount, d.DueDate }
        ).ToListAsync(cancellationToken);

        // Tahakkuk detaylarına yapılan tahsilat dağıtımları (FAZ 7)
        var detailIds = rows.Select(r => r.Id).ToList();
        var paidMap = (await _db.CollectionAllocations
            .Where(al => detailIds.Contains(al.AccrualDetailId) && !al.IsDeleted)
            .GroupBy(al => al.AccrualDetailId)
            .Select(g => new { DetailId = g.Key, Sum = g.Sum(x => x.AllocatedAmount) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DetailId, x => x.Sum);

        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var totalAccrued = rows.Sum(r => r.Amount);
        var paid = rows.Sum(r => paidMap.GetValueOrDefault(r.Id, 0m));

        // Kalan = her detayın (tutar - ödenen); vadesi geçen kalan ayrıca
        decimal remaining = 0m, overdue = 0m;
        DateOnly? earliestUnpaidDue = null;
        foreach (var r in rows)
        {
            var rem = r.Amount - paidMap.GetValueOrDefault(r.Id, 0m);
            if (rem <= 0m) continue;
            remaining += rem;
            if (r.DueDate < today) overdue += rem;
            if (earliestUnpaidDue is null || r.DueDate < earliestUnpaidDue) earliestUnpaidDue = r.DueDate;
        }

        var status = new UnitDebtStatus(
            request.UnitId,
            totalAccrued,
            overdue,
            rows.Count,
            earliestUnpaidDue,
            PaidAmount: paid,
            RemainingAmount: remaining);

        return Result<UnitDebtStatus>.Success(status);
    }
}
