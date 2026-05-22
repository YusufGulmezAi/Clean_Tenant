using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// Mizan raporunu getirir — hesap bazlı borç/alacak/bakiye özeti.
/// <para>
/// <paramref name="Month"/> null ise mali yılın tamamı raporlanır.
/// </para>
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetTrialBalanceQuery(
    Guid CompanyId,
    Guid FiscalYearId,
    int? Month) : IRequest<Result<TrialBalanceReport>>;
