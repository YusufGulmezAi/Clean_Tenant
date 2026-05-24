using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary>
/// Taslak bütçe versiyonundan bir kalem versiyonunu kaldırır (soft-delete + taksitleri).
/// Yalnız <b>Draft</b> versiyonda izinlidir.
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record RemoveBudgetLineVersionCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid BudgetLineVersionId) : IRequest<Result>;
