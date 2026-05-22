using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// Hesap ekstresi raporunu getirir — hesap bazlı tarih aralıklı hareket listesi.
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetAccountStatementQuery(
    Guid CompanyId,
    string AccountCode,
    DateOnly From,
    DateOnly To) : IRequest<Result<IReadOnlyList<AccountStatementEntry>>>;
