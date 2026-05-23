using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Invoices;

/// <summary>
/// <see cref="PostInvoiceToJournalCommand"/> handler.
/// <para>
/// Faturayı yevmiyeye aktarır: hesap kodlarını DB'den okur, borç/alacak
/// satırlarını oluşturur, EntrySequence'i artırır ve Invoice'u günceller.
/// </para>
/// </summary>
public sealed class PostInvoiceToJournalCommandHandler
    : IRequestHandler<PostInvoiceToJournalCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public PostInvoiceToJournalCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        PostInvoiceToJournalCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Faturayı getir
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(inv => inv.Id == command.InvoiceId
                                     && inv.CompanyId == command.CompanyId
                                     && !inv.IsDeleted, cancellationToken);

        if (invoice is null)
            return Result<Guid>.Failure(
                Error.NotFound("ACC-503", "Fatura bulunamadı."));

        // 2. Tekrar aktarım kontrolü
        if (invoice.IsPostedToJournal)
            return Result<Guid>.Failure(
                Error.Failure("ACC-504", "Fatura zaten yevmiyeye aktarılmış."));

        // 3. Muhasebe dönemi kontrolü
        var period = await _db.AccountingPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.Id == invoice.AccountingPeriodId
                                   && p.CompanyId == command.CompanyId
                                   && !p.IsDeleted, cancellationToken);

        if (period is null)
            return Result<Guid>.Failure(
                Error.NotFound("ACC-004", "Muhasebe dönemi bulunamadı."));

        if (period.Status != PeriodStatus.Open)
            return Result<Guid>.Failure(
                Error.Failure("ACC-218", "Kapalı döneme fiş yazılamaz."));

        // 4. Faturanın bağlı olduğu hesap kodunu getir (gider/gelir hesabı)
        var invoiceAccountCode = await _db.AccountCodes
            .FirstOrDefaultAsync(ac => ac.Id == invoice.AccountCodeId
                                    && ac.CompanyId == command.CompanyId
                                    && !ac.IsDeleted, cancellationToken);

        if (invoiceAccountCode is null)
            return Result<Guid>.Failure(
                Error.NotFound("ACC-001", "Fatura hesap kodu bulunamadı."));

        // 5. Sabit hesap kodlarını getir (Satıcılar / Alıcılar)
        var counterpartyCode = invoice.Direction == InvoiceDirection.Incoming
            ? "320.001.001"  // Satıcılar
            : "120.001.001"; // Alıcılar

        var counterpartyAccountCode = await _db.AccountCodes
            .FirstOrDefaultAsync(ac => ac.CompanyId == command.CompanyId
                                    && ac.Code == counterpartyCode
                                    && !ac.IsDeleted, cancellationToken);

        if (counterpartyAccountCode is null)
            return Result<Guid>.Failure(
                Error.Failure("ACC-501", $"Cari hesap kodu bulunamadı: {counterpartyCode}"));

        // 6. KDV hesap kodunu belirle (VatCategory'ye göre)
        AccountCode? vatAccountCode = null;
        if (invoice.VatCategory != VatCategory.NoVat && invoice.VatAmount > 0)
        {
            var vatCode = GetVatAccountCode(invoice.Direction, invoice.VatCategory);
            vatAccountCode = await _db.AccountCodes
                .FirstOrDefaultAsync(ac => ac.CompanyId == command.CompanyId
                                        && ac.Code == vatCode
                                        && !ac.IsDeleted, cancellationToken);

            if (vatAccountCode is null)
                return Result<Guid>.Failure(
                    Error.Failure("ACC-501", $"KDV hesap kodu bulunamadı: {vatCode}"));
        }

        // 7. EntrySequence artır (Normal fiş tipi)
        var sequence = await _db.EntrySequences
            .FirstOrDefaultAsync(es => es.CompanyId == command.CompanyId
                                    && es.FiscalYearId == period.FiscalYearId
                                    && es.EntryType == EntryType.Normal
                                    && !es.IsDeleted, cancellationToken);

        if (sequence is null)
        {
            sequence = new EntrySequence
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                FiscalYearId = period.FiscalYearId,
                EntryType = EntryType.Normal,
                LastNumber = 0
            };
            _db.EntrySequences.Add(sequence);
        }

        sequence.LastNumber++;
        var entryNumber = $"{period.FiscalYear.Label}/{sequence.LastNumber:D6}";

        // 8. Yevmiye fişini oluştur
        var entry = new JournalEntry
        {
            TenantId = command.TenantId,
            CompanyId = command.CompanyId,
            AccountingPeriodId = invoice.AccountingPeriodId,
            EntryType = EntryType.Normal,
            EntryNumber = entryNumber,
            EntryDate = invoice.InvoiceDate,
            Description = $"Fatura: {invoice.InvoiceNumber} — {invoice.CounterpartyName}",
            Reference = invoice.InvoiceNumber,
            ReferenceId = invoice.Id,
            TotalDebit = invoice.TotalAmount,
            TotalCredit = invoice.TotalAmount,
            Status = JournalEntryStatus.Posted,
            PostedAt = DateTimeOffset.UtcNow
        };

        // 9. Yevmiye satırlarını oluştur
        var lines = BuildJournalLines(
            command, invoice, invoiceAccountCode, counterpartyAccountCode, vatAccountCode, entry);

        entry.Lines = lines;

        _db.JournalEntries.Add(entry);

        // 10. Faturayı güncelle
        invoice.IsPostedToJournal = true;
        invoice.JournalEntryId = entry.Id;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entry.Id);
    }

    // ── Yardımcı metodlar ────────────────────────────────────────────────────

    private static string GetVatAccountCode(InvoiceDirection direction, VatCategory vatCategory)
    {
        // Gelen fatura → İndirilecek KDV (191.xx.001)
        // Giden fatura → Hesaplanan KDV  (391.xx.001)
        var prefix = direction == InvoiceDirection.Incoming ? "191" : "391";

        return vatCategory switch
        {
            VatCategory.Vat1  => $"{prefix}.01.001",
            VatCategory.Vat10 => $"{prefix}.10.001",
            VatCategory.Vat20 => $"{prefix}.20.001",
            _                 => throw new InvalidOperationException($"Beklenmeyen VatCategory: {vatCategory}")
        };
    }

    private static List<JournalLine> BuildJournalLines(
        PostInvoiceToJournalCommand command,
        Domain.Tenant.Accounting.Invoice invoice,
        AccountCode invoiceAccountCode,
        AccountCode counterpartyAccountCode,
        AccountCode? vatAccountCode,
        JournalEntry entry)
    {
        var lines = new List<JournalLine>();

        if (invoice.Direction == InvoiceDirection.Incoming)
        {
            // Gelen fatura:
            // Borç: gider hesabı = SubTotal
            lines.Add(new JournalLine
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                JournalEntry = entry,
                AccountCodeId = invoiceAccountCode.Id,
                AccountCodeValue = invoiceAccountCode.Code,
                Debit = invoice.SubTotal,
                Credit = 0,
                Description = $"Gider — {invoice.InvoiceNumber}"
            });

            // Borç: İndirilecek KDV = VatAmount (varsa)
            if (vatAccountCode is not null && invoice.VatAmount > 0)
            {
                lines.Add(new JournalLine
                {
                    TenantId = command.TenantId,
                    CompanyId = command.CompanyId,
                    JournalEntry = entry,
                    AccountCodeId = vatAccountCode.Id,
                    AccountCodeValue = vatAccountCode.Code,
                    Debit = invoice.VatAmount,
                    Credit = 0,
                    Description = $"İndirilecek KDV — {invoice.InvoiceNumber}"
                });
            }

            // Alacak: Satıcılar = TotalAmount
            lines.Add(new JournalLine
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                JournalEntry = entry,
                AccountCodeId = counterpartyAccountCode.Id,
                AccountCodeValue = counterpartyAccountCode.Code,
                Debit = 0,
                Credit = invoice.TotalAmount,
                Description = $"Satıcı borcu — {invoice.CounterpartyName}"
            });
        }
        else
        {
            // Giden fatura:
            // Borç: Alıcılar = TotalAmount
            lines.Add(new JournalLine
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                JournalEntry = entry,
                AccountCodeId = counterpartyAccountCode.Id,
                AccountCodeValue = counterpartyAccountCode.Code,
                Debit = invoice.TotalAmount,
                Credit = 0,
                Description = $"Alıcı alacağı — {invoice.CounterpartyName}"
            });

            // Alacak: gelir hesabı = SubTotal
            lines.Add(new JournalLine
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                JournalEntry = entry,
                AccountCodeId = invoiceAccountCode.Id,
                AccountCodeValue = invoiceAccountCode.Code,
                Debit = 0,
                Credit = invoice.SubTotal,
                Description = $"Gelir — {invoice.InvoiceNumber}"
            });

            // Alacak: Hesaplanan KDV = VatAmount (varsa)
            if (vatAccountCode is not null && invoice.VatAmount > 0)
            {
                lines.Add(new JournalLine
                {
                    TenantId = command.TenantId,
                    CompanyId = command.CompanyId,
                    JournalEntry = entry,
                    AccountCodeId = vatAccountCode.Id,
                    AccountCodeValue = vatAccountCode.Code,
                    Debit = 0,
                    Credit = invoice.VatAmount,
                    Description = $"Hesaplanan KDV — {invoice.InvoiceNumber}"
                });
            }
        }

        return lines;
    }
}
