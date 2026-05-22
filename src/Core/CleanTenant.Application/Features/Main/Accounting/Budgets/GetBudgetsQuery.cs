using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Budgets;

/// <summary>
/// Şirkete ait bütçe kalemlerini listeler.
/// <para>
/// <paramref name="AccountingPeriodId"/> belirtilirse yalnızca o döneme ait bütçeler döner.
/// </para>
/// </summary>
[RequirePermission("company.accounting.budget.read")]
public sealed record GetBudgetsQuery(
    Guid CompanyId,
    Guid? AccountingPeriodId = null,
    Guid? CostCenterId = null) : IRequest<Result<IReadOnlyList<BudgetListItem>>>;
