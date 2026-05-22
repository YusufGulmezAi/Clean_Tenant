using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>Şirkete ait yıllık bütçeleri listeler.</summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetBudgetsByCompanyQuery(
    Guid CompanyId) : IRequest<Result<IReadOnlyList<BudgetListItem>>>;

/// <summary>Bütçe liste öğesi.</summary>
public sealed record BudgetListItem(
    Guid Id,
    Guid FiscalYearId,
    string FiscalYearLabel,
    string Title,
    BudgetStatus Status,
    Guid? CurrentVersionId,
    int VersionCount,
    DateTimeOffset CreatedAt);
