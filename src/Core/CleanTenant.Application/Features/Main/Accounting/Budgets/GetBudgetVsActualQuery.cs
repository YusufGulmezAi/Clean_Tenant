using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Budgets;

/// <summary>
/// Bütçe-gerçekleşme karşılaştırma raporunu getirir.
/// <para>
/// <paramref name="Month"/> null ise tüm mali yıl bazında karşılaştırma yapılır.
/// </para>
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetBudgetVsActualQuery(
    Guid CompanyId,
    Guid FiscalYearId,
    int? Month) : IRequest<Result<BudgetVsActualReport>>;
