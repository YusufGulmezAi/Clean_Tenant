using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// Bilanço raporunu getirir — belirli bir tarihe göre aktif/pasif dengesi.
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetBalanceSheetQuery(
    Guid CompanyId,
    DateOnly AsOf) : IRequest<Result<BalanceSheetReport>>;
