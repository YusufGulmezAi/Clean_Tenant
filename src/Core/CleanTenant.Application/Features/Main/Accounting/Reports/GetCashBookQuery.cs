using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// Kasa/banka defteri raporunu getirir — belirli nakit hesabının hareket listesi.
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetCashBookQuery(
    Guid CompanyId,
    string AccountCode,
    DateOnly From,
    DateOnly To) : IRequest<Result<IReadOnlyList<CashBookEntry>>>;
