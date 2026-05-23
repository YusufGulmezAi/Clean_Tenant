using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.LateFees.Queries;

/// <summary>Bir şirketin gecikme faizi politikalarını (varsayılan + bütçe override) listeler.</summary>
[RequirePermission("tenant.latefee.configure")]
public sealed record GetLateFeePoliciesQuery(
    Guid CompanyId) : IRequest<Result<IReadOnlyList<LateFeePolicyItem>>>;

/// <summary>Gecikme faizi politikası liste öğesi.</summary>
public sealed record LateFeePolicyItem(
    Guid Id,
    Guid? BudgetId,
    string? BudgetTitle,
    decimal MonthlyRatePercent,
    bool IsCompound,
    int GraceDays,
    Guid IncomeAccountCodeId,
    string? IncomeAccountCode,
    bool IsActive);
