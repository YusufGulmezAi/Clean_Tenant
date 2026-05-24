using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// Taslak bütçeyi siler (soft-delete: bütçe + versiyonları + kalem versiyonları +
/// taksitleri). Yalnız <b>Draft</b> bütçe silinebilir; yayınlı bütçenin tahakkuk/
/// yevmiye bağı olabileceğinden silinmez.
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record DeleteBudgetCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid BudgetId) : IRequest<Result>;
