using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// Büyük defter raporunu getirir — belirli hesap kodu için tarih aralıklı hareketler.
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetGeneralLedgerQuery(
    Guid CompanyId,
    string AccountCode,
    DateOnly From,
    DateOnly To) : IRequest<Result<IReadOnlyList<GeneralLedgerEntry>>>;
