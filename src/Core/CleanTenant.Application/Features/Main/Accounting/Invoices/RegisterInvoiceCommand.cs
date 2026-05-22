using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Invoices;

/// <summary>
/// Sisteme yeni fatura kaydeder (gelen veya giden).
/// <para>
/// Bu komut yalnızca faturayı kaydeder; yevmiyeye aktarım için
/// <see cref="PostInvoiceToJournalCommand"/> kullanılır.
/// </para>
/// </summary>
[RequirePermission("company.accounting.invoice.write")]
public sealed record RegisterInvoiceCommand(
    Guid CompanyId,
    Guid TenantId,
    Guid AccountingPeriodId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    InvoiceDirection Direction,
    string CounterpartyName,
    string? CounterpartyTaxId,
    Guid AccountCodeId,
    decimal SubTotal,
    VatCategory VatCategory,
    decimal VatAmount,
    string? Notes) : IRequest<Result<InvoiceDetail>>;
