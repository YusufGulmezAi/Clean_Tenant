using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Budgets;

/// <summary>
/// <see cref="GetBudgetsQuery"/> handler. Şirkete ait bütçe kalemlerini listeler.
/// </summary>
public sealed class GetBudgetsQueryHandler
    : IRequestHandler<GetBudgetsQuery, Result<IReadOnlyList<BudgetListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBudgetsQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BudgetListItem>>> Handle(
        GetBudgetsQuery query,
        CancellationToken cancellationToken)
    {
        var q = _db.Budgets
            .Where(b => b.CompanyId == query.CompanyId && !b.IsDeleted);

        if (query.AccountingPeriodId.HasValue)
            q = q.Where(b => b.AccountingPeriodId == query.AccountingPeriodId.Value);

        if (query.CostCenterId.HasValue)
            q = q.Where(b => b.CostCenterId == query.CostCenterId.Value);

        var items = await q
            .Join(
                _db.AccountCodes.Where(ac => !ac.IsDeleted),
                b => b.AccountCodeId,
                ac => ac.Id,
                (b, ac) => new { Budget = b, AccountCode = ac })
            .GroupJoin(
                _db.CostCenters.Where(cc => !cc.IsDeleted),
                x => x.Budget.CostCenterId,
                cc => (Guid?)cc.Id,
                (x, ccs) => new { x.Budget, x.AccountCode, CostCenters = ccs })
            .SelectMany(
                x => x.CostCenters.DefaultIfEmpty(),
                (x, cc) => new BudgetListItem(
                    x.Budget.Id,
                    x.Budget.AccountingPeriodId,
                    x.Budget.AccountCodeId,
                    x.AccountCode.Code,
                    x.AccountCode.Name,
                    x.Budget.CostCenterId,
                    cc != null ? cc.Name : null,
                    x.Budget.BudgetedAmount))
            .OrderBy(b => b.AccountCode)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BudgetListItem>>.Success(items.AsReadOnly());
    }
}
