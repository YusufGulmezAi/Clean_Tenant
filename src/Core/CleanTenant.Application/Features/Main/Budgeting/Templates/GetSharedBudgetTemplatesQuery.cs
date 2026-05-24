using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary>
/// Çağıran tenant'ın erişebileceği bütçe şablonlarını listeler: Public olanlar +
/// sistem küratörlü (OwnerTenantId = null) + kendi Private şablonları. Opsiyonel
/// <see cref="Type"/> filtresi (site bütçe türüne göre).
/// </summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetSharedBudgetTemplatesQuery(
    BudgetType? Type = null) : IRequest<Result<IReadOnlyList<BudgetTemplateListItem>>>;

/// <summary>Bütçe şablonu liste öğesi.</summary>
public sealed record BudgetTemplateListItem(
    Guid Id,
    string UrlCode,
    Guid? OwnerTenantId,
    TemplateVisibility Visibility,
    BudgetType Type,
    string Name,
    string? Description,
    int LineCount,
    DateTimeOffset CreatedAt);
