using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary>
/// Bir taslak bütçe versiyonundaki kalem versiyonunu günceller (tutar/ödeme planı/
/// dağıtım/katılım grubu/vade/taksit yapılandırması). Yalnız <b>Draft</b> versiyonda
/// (PublishedAt == null) izinlidir; yayınlı versiyon revize edilmelidir.
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record UpdateBudgetLineVersionCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid BudgetLineVersionId,
    decimal PlannedAmount,
    PaymentSchedule PaymentSchedule,
    DistributionModel DistributionModel,
    Guid? ParticipationGroupId,
    string? DistributionConfig,
    int DueDayOfMonth,
    int? InstallmentStartYear = null,
    int? InstallmentStartMonth = null,
    int? InstallmentEndYear = null,
    int? InstallmentEndMonth = null,
    int? InstallmentIntervalMonths = null) : IRequest<Result>;
