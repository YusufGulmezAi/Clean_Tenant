using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary>
/// Bir BudgetVersion (genellikle Draft) içine bir <c>BudgetLine</c> için planlanan
/// tutar + dağıtım + ödeme planı atar. Yayınlanmış (published) versiyona ekleme
/// yapılamaz (BDG-401); revizyon için <c>ReviseBudgetCommand</c> kullanılır.
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record AddBudgetLineToVersionCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid BudgetVersionId,
    Guid BudgetLineId,
    decimal PlannedAmount,
    PaymentSchedule PaymentSchedule,
    DistributionModel DistributionModel,
    Guid? ParticipationGroupId = null,
    string? DistributionConfig = null,
    int DueDayOfMonth = 15) : IRequest<Result<Guid>>;
