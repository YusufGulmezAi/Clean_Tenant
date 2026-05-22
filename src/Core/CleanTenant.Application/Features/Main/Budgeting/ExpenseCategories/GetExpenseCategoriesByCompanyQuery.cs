using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.ExpenseCategories;

/// <summary>Şirkete ait gider kategorilerini düz liste olarak getirir; UI ağaç görünümünü kendi inşa eder.</summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetExpenseCategoriesByCompanyQuery(
    Guid CompanyId) : IRequest<Result<IReadOnlyList<ExpenseCategoryListItem>>>;

/// <summary>Gider kategorisi düz liste öğesi.</summary>
public sealed record ExpenseCategoryListItem(
    Guid Id,
    Guid? ParentCategoryId,
    string Code,
    string Name,
    string? Description,
    int DisplayOrder,
    int LineCount);
