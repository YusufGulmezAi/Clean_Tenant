using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>
/// <see cref="GetCostCentersQuery"/> handler.
/// </summary>
public sealed class GetCostCentersQueryHandler
    : IRequestHandler<GetCostCentersQuery, Result<IReadOnlyList<CostCenterListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetCostCentersQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CostCenterListItem>>> Handle(
        GetCostCentersQuery query,
        CancellationToken cancellationToken)
    {
        var q = _db.CostCenters
            .Where(cc => cc.CompanyId == query.CompanyId && !cc.IsDeleted);

        if (query.OnlyActive)
            q = q.Where(cc => cc.IsActive);

        var items = await q
            .OrderBy(cc => cc.Code)
            .Select(cc => new CostCenterListItem(
                cc.Id,
                cc.Code,
                cc.Name,
                cc.Description,
                cc.IsActive))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CostCenterListItem>>.Success(items.AsReadOnly());
    }
}
