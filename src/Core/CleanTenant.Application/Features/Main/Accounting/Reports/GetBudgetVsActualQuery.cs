using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// Bütçe-gerçekleşme karşılaştırma raporunu getirir — mali yıl ve opsiyonel ay bazlı sapma analizi.
/// <para>
/// <paramref name="Month"/> null ise mali yılın tamamı karşılaştırılır.
/// </para>
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetBudgetVsActualQuery(
    Guid CompanyId,
    Guid FiscalYearId,
    int? Month) : IRequest<Result<BudgetVsActualReport>>;
