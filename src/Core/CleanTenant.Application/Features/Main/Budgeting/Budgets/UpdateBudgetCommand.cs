using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// Taslak bütçenin başlık/not/dönem bilgisini günceller. Yalnız <b>Draft</b>
/// bütçede izinlidir (yayınlı bütçe revize edilir). Dönem alanları verilmezse korunur.
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record UpdateBudgetCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid BudgetId,
    string Title,
    string? Notes = null,
    int? PeriodStartYear = null,
    int? PeriodStartMonth = null,
    int? PeriodEndYear = null,
    int? PeriodEndMonth = null) : IRequest<Result>;
