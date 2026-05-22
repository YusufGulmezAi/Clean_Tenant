using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary>
/// Belirli bir bütçe versiyonundaki kalem detaylarını (kalem adı + planlanan
/// tutar + dağıtım) listeler.
/// </summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetBudgetVersionLinesQuery(
    Guid CompanyId,
    Guid BudgetVersionId) : IRequest<Result<IReadOnlyList<BudgetLineVersionDto>>>;

/// <summary>Bütçe versiyonundaki bir satırın özeti (kalem ile birleşik).</summary>
public sealed record BudgetLineVersionDto(
    Guid Id,
    Guid BudgetLineId,
    string LineCode,
    string LineName,
    Guid ExpenseCategoryId,
    string CategoryCode,
    string CategoryName,
    decimal PlannedAmount,
    PaymentSchedule PaymentSchedule,
    DistributionModel DistributionModel,
    Guid? ParticipationGroupId,
    string? ParticipationGroupName,
    bool IsManualOverride,
    string? OverrideReason,
    int DueDayOfMonth);
