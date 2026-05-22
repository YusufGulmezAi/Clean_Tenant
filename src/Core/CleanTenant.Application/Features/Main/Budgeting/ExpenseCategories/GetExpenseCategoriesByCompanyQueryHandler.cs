using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.ExpenseCategories;

/// <summary><see cref="GetExpenseCategoriesByCompanyQuery"/> handler.</summary>
public sealed class GetExpenseCategoriesByCompanyQueryHandler
    : IRequestHandler<GetExpenseCategoriesByCompanyQuery, Result<IReadOnlyList<ExpenseCategoryListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetExpenseCategoriesByCompanyQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExpenseCategoryListItem>>> Handle(
        GetExpenseCategoriesByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _db.ExpenseCategories
            .Where(c => c.CompanyId == request.CompanyId && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Code)
            .Select(c => new ExpenseCategoryListItem(
                c.Id,
                c.ParentCategoryId,
                c.Code,
                c.Name,
                c.Description,
                c.DisplayOrder,
                _db.BudgetLines.Count(l => l.ExpenseCategoryId == c.Id && !l.IsDeleted)))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ExpenseCategoryListItem>>.Success(items.AsReadOnly());
    }
}
