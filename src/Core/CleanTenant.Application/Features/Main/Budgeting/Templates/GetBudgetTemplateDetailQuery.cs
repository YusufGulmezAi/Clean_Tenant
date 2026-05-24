using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary>Bir bütçe şablonunun kalemleriyle önizlemesi (erişim görünürlüğü kontrol edilir).</summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetBudgetTemplateDetailQuery(
    Guid TemplateId) : IRequest<Result<BudgetTemplateDetail>>;

/// <summary>Bütçe şablonu detayı.</summary>
public sealed record BudgetTemplateDetail(
    Guid Id,
    string UrlCode,
    Guid? OwnerTenantId,
    TemplateVisibility Visibility,
    BudgetType Type,
    string Name,
    string? Description,
    IReadOnlyList<BudgetTemplateLineItem> Lines);

/// <summary>Şablon kalemi önizleme öğesi (yapı-only).</summary>
public sealed record BudgetTemplateLineItem(
    string CategoryCode,
    string CategoryName,
    string LineCode,
    string LineName,
    PaymentSchedule PaymentSchedule,
    DistributionModel DistributionModel,
    int DueDayOfMonth,
    string? ParticipationGroupName,
    int? InstallmentIntervalMonths,
    int? InstallmentCount);
