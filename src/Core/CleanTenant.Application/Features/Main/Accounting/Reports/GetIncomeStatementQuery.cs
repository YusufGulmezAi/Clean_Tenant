using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// Gelir tablosu raporunu getirir — tarih aralığı gelir/gider/net kâr özeti.
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetIncomeStatementQuery(
    Guid CompanyId,
    DateOnly From,
    DateOnly To) : IRequest<Result<IncomeStatementReport>>;
