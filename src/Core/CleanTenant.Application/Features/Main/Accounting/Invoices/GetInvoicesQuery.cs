using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Invoices;

/// <summary>
/// Şirkete ait faturaları filtreli listeler.
/// <para>
/// <paramref name="Direction"/> null ise hem gelen hem giden faturalar döner.
/// <paramref name="OnlyUnposted"/> = true ise yalnızca henüz yevmiyeye aktarılmamış faturalar listelenir.
/// </para>
/// </summary>
[RequirePermission("company.accounting.invoice.read")]
public sealed record GetInvoicesQuery(
    Guid CompanyId,
    InvoiceDirection? Direction = null,
    bool OnlyUnposted = false,
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<Result<IReadOnlyList<InvoiceListItem>>>;
