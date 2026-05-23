using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accruals.Queries;

/// <summary><see cref="GetUnitAccrualHistoryQuery"/> handler.</summary>
public sealed class GetUnitAccrualHistoryQueryHandler
    : IRequestHandler<GetUnitAccrualHistoryQuery, Result<IReadOnlyList<UnitAccrualItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUnitAccrualHistoryQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UnitAccrualItem>>> Handle(
        GetUnitAccrualHistoryQuery request, CancellationToken cancellationToken)
    {
        var q = from d in _db.AccrualDetails
                join a in _db.Accruals on d.AccrualId equals a.Id
                where d.UnitId == request.UnitId
                    && a.CompanyId == request.CompanyId
                    && !d.IsDeleted && !a.IsDeleted
                select new { Detail = d, Accrual = a };

        if (request.From is { } from)
            q = q.Where(x => x.Detail.DueDate >= from);
        if (request.To is { } to)
            q = q.Where(x => x.Detail.DueDate <= to);

        var items = await q
            .OrderByDescending(x => x.Accrual.Year).ThenByDescending(x => x.Accrual.Month)
            .Select(x => new UnitAccrualItem(
                x.Detail.Id,
                x.Accrual.Id,
                x.Accrual.Source,
                x.Accrual.Year,
                x.Accrual.Month,
                x.Accrual.Description,
                x.Detail.Amount,
                x.Detail.DueDate,
                x.Detail.LineBreakdownJson))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UnitAccrualItem>>.Success(items.AsReadOnly());
    }
}
