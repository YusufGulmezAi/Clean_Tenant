using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary><see cref="GetBudgetsByCompanyQuery"/> handler.</summary>
public sealed class GetBudgetsByCompanyQueryHandler
    : IRequestHandler<GetBudgetsByCompanyQuery, Result<IReadOnlyList<BudgetListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBudgetsByCompanyQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BudgetListItem>>> Handle(
        GetBudgetsByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        var items = await (
            from b in _db.Budgets
            join fy in _db.FiscalYears on b.FiscalYearId equals fy.Id
            where b.CompanyId == request.CompanyId && !b.IsDeleted && !fy.IsDeleted
            orderby fy.StartDate descending
            select new BudgetListItem(
                b.Id,
                b.FiscalYearId,
                fy.Label,
                b.Title,
                b.Status,
                b.CurrentVersionId,
                b.Versions.Count(v => !v.IsDeleted),
                b.CreatedAt)
        ).ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BudgetListItem>>.Success(items.AsReadOnly());
    }
}
