using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Invoices;

/// <summary>
/// <see cref="RegisterInvoiceCommand"/> handler.
/// </summary>
public sealed class RegisterInvoiceCommandHandler
    : IRequestHandler<RegisterInvoiceCommand, Result<InvoiceDetail>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RegisterInvoiceCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<InvoiceDetail>> Handle(
        RegisterInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        // Fatura numarası benzersizliği kontrolü (aynı şirket içinde)
        var numberExists = await _db.Invoices
            .AnyAsync(inv => inv.CompanyId == command.CompanyId
                          && inv.InvoiceNumber == command.InvoiceNumber
                          && !inv.IsDeleted, cancellationToken);

        if (numberExists)
            return Result<InvoiceDetail>.Failure(
                Error.Conflict("ACC-502", $"'{command.InvoiceNumber}' fatura numarası zaten kayıtlı."));

        // Muhasebe dönemi kontrolü
        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.Id == command.AccountingPeriodId
                                   && p.CompanyId == command.CompanyId
                                   && !p.IsDeleted, cancellationToken);

        if (period is null)
            return Result<InvoiceDetail>.Failure(
                Error.NotFound("ACC-004", "Muhasebe dönemi bulunamadı."));

        if (period.Status != Domain.Tenant.Accounting.Enums.PeriodStatus.Open)
            return Result<InvoiceDetail>.Failure(
                Error.Failure("ACC-218", "Kapalı döneme fatura kaydedilemez."));

        var totalAmount = command.SubTotal + command.VatAmount;

        var invoice = new Invoice
        {
            TenantId = command.TenantId,
            CompanyId = command.CompanyId,
            AccountingPeriodId = command.AccountingPeriodId,
            InvoiceNumber = command.InvoiceNumber,
            InvoiceDate = command.InvoiceDate,
            DueDate = command.DueDate,
            Direction = command.Direction,
            CounterpartyName = command.CounterpartyName,
            CounterpartyTaxId = command.CounterpartyTaxId,
            AccountCodeId = command.AccountCodeId,
            SubTotal = command.SubTotal,
            VatCategory = command.VatCategory,
            VatAmount = command.VatAmount,
            TotalAmount = totalAmount,
            IsPostedToJournal = false,
            Notes = command.Notes
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<InvoiceDetail>.Success(new InvoiceDetail(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.Direction,
            invoice.CounterpartyName,
            invoice.CounterpartyTaxId,
            invoice.AccountCodeId,
            invoice.SubTotal,
            invoice.VatCategory,
            invoice.VatAmount,
            invoice.TotalAmount,
            invoice.IsPostedToJournal,
            invoice.JournalEntryId,
            invoice.Notes));
    }
}
