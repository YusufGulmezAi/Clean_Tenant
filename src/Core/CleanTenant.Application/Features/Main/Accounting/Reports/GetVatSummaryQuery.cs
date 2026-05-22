using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// KDV özet raporunu getirir — belirli ay için indirilecek/hesaplanan/ödenecek KDV.
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetVatSummaryQuery(
    Guid CompanyId,
    int Year,
    int Month) : IRequest<Result<VatSummaryReport>>;
