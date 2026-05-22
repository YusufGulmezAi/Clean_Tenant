using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.ExemptionRules;

/// <summary>Şirkete ait muafiyet kurallarını listeler (BB + kalem adı dahil).</summary>
[RequirePermission("tenant.budget.view")]
public sealed record GetExemptionRulesByCompanyQuery(
    Guid CompanyId,
    Guid? UnitId = null,
    Guid? BudgetLineId = null) : IRequest<Result<IReadOnlyList<ExemptionRuleListItem>>>;

/// <summary>Muafiyet kuralı liste öğesi.</summary>
public sealed record ExemptionRuleListItem(
    Guid Id,
    Guid UnitId,
    string UnitNumber,
    Guid BudgetLineId,
    string BudgetLineCode,
    string BudgetLineName,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string Reason);
