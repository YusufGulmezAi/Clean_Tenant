using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLines;

/// <summary><see cref="GetBudgetLinesByCompanyQuery"/> handler.</summary>
public sealed class GetBudgetLinesByCompanyQueryHandler
    : IRequestHandler<GetBudgetLinesByCompanyQuery, Result<IReadOnlyList<BudgetLineListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBudgetLinesByCompanyQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BudgetLineListItem>>> Handle(
        GetBudgetLinesByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        var q = from l in _db.BudgetLines
                join c in _db.ExpenseCategories on l.ExpenseCategoryId equals c.Id
                where l.CompanyId == request.CompanyId && !l.IsDeleted && !c.IsDeleted
                select new { Line = l, Category = c };

        if (request.OnlyActive)
            q = q.Where(x => x.Line.IsActive);

        var items = await q
            .OrderBy(x => x.Category.DisplayOrder)
            .ThenBy(x => x.Line.DisplayOrder)
            .ThenBy(x => x.Line.Code)
            .Select(x => new BudgetLineListItem(
                x.Line.Id,
                x.Line.Code,
                x.Line.Name,
                x.Line.Description,
                x.Line.ExpenseCategoryId,
                x.Category.Code,
                x.Category.Name,
                x.Line.AccountCodeId,
                x.Line.IsActive,
                x.Line.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BudgetLineListItem>>.Success(items.AsReadOnly());
    }
}
