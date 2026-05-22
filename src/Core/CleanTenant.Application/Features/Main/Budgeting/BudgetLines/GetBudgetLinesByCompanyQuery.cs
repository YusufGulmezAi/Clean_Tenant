using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLines;

/// <summary>Şirkete ait bütçe kalemi tanımlarını listeler.</summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetBudgetLinesByCompanyQuery(
    Guid CompanyId,
    bool OnlyActive = true) : IRequest<Result<IReadOnlyList<BudgetLineListItem>>>;

/// <summary>Bütçe kalemi liste öğesi.</summary>
public sealed record BudgetLineListItem(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid ExpenseCategoryId,
    string CategoryCode,
    string CategoryName,
    Guid? AccountCodeId,
    bool IsActive,
    int DisplayOrder);
