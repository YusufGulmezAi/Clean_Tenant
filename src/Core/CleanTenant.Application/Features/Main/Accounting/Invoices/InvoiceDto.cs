using CleanTenant.Domain.Tenant.Accounting.Enums;

namespace CleanTenant.Application.Features.Main.Accounting.Invoices;

/// <summary>Fatura liste elemanı — GetInvoicesQuery dönüş tipi.</summary>
public record InvoiceListItem(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    InvoiceDirection Direction,
    string CounterpartyName,
    decimal TotalAmount,
    bool IsPostedToJournal);

/// <summary>Fatura tam detay — GetInvoiceDetailQuery dönüş tipi.</summary>
public record InvoiceDetail(
    Guid Id,
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
    decimal TotalAmount,
    bool IsPostedToJournal,
    Guid? JournalEntryId,
    string? Notes);
