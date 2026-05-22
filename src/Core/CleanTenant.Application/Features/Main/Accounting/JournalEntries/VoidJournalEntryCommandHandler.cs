using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="VoidJournalEntryCommand"/> handler.
/// <para>
/// VUK gereği ters fiş (storno) mekanizması:
/// <list type="number">
///   <item>Orijinal fişi ve satırlarını çek</item>
///   <item>Status doğrulaması (yalnızca Posted iptale açık)</item>
///   <item>Dönem açıklık kontrolü (kilitli dönemde iptal yapılamaz)</item>
///   <item>Ters fiş oluştur (Debit ↔ Credit tersine çevrilir)</item>
///   <item>Orijinal fişi Voided durumuna taşı</item>
///   <item>İkisi tek SaveChangesAsync'te persist edilir</item>
/// </list>
/// </para>
/// </summary>
public sealed class VoidJournalEntryCommandHandler
    : IRequestHandler<VoidJournalEntryCommand, Result<JournalEntryCreated>>
{
    private readonly IMainDbContext _db;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public VoidJournalEntryCommandHandler(
        IMainDbContext db,
        ICurrentSessionAccessor sessionAccessor)
    {
        _db = db;
        _sessionAccessor = sessionAccessor;
    }

    /// <inheritdoc />
    public async Task<Result<JournalEntryCreated>> Handle(
        VoidJournalEntryCommand cmd,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException(
                "ICurrentSessionAccessor.Current null. Endpoint korumalı olmalı.");

        // 1. Orijinal fişi satırlarla çek
        var entry = await _db.JournalEntries
            .Include(je => je.Lines)
            .FirstOrDefaultAsync(
                je => je.Id == cmd.EntryId
                   && je.CompanyId == cmd.CompanyId
                   && !je.IsDeleted, cancellationToken);

        if (entry is null)
            return Result<JournalEntryCreated>.Failure(
                Error.NotFound("ACC-004", "Yevmiye fişi bulunamadı."));

        // 2. Durum kontrolü
        if (entry.Status == JournalEntryStatus.Draft)
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-302", "Taslak fiş iptal edilemez. Fişi silin."));

        if (entry.Status == JournalEntryStatus.PendingApproval)
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-303", "Onay bekleyen fiş iptal edilemez — önce reddedin."));

        if (entry.Status == JournalEntryStatus.Voided)
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-304", "Fiş zaten iptal edilmiş."));

        // 3. Dönemi çek ve açıklık kontrolü
        var period = await _db.AccountingPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.Id == entry.AccountingPeriodId, cancellationToken);

        if (period is null)
            return Result<JournalEntryCreated>.Failure(
                Error.NotFound("ACC-004", "Muhasebe dönemi bulunamadı."));

        if (period.Status != PeriodStatus.Open)
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-305", "Dönem kilitli; iptal işlemi gerçekleştirilemiyor."));

        // 4. EntrySequence — ters fiş için yeni numara üret
        var sequence = await _db.EntrySequences
            .FirstOrDefaultAsync(
                es => es.CompanyId == cmd.CompanyId
                   && es.FiscalYearId == period.FiscalYearId
                   && es.EntryType == EntryType.Correction, cancellationToken);

        if (sequence is null)
        {
            sequence = new EntrySequence
            {
                TenantId = cmd.TenantId,
                CompanyId = cmd.CompanyId,
                FiscalYearId = period.FiscalYearId,
                EntryType = EntryType.Correction,
                LastNumber = 0
            };
            _db.EntrySequences.Add(sequence);
        }

        sequence.LastNumber++;
        var reversalEntryNumber = $"{period.FiscalYear.Label}/{sequence.LastNumber:D6}";

        var now = DateTimeOffset.UtcNow;

        // 5. Ters fiş oluştur (Debit ↔ Credit tersine çevrilir)
        var reversalEntry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            CompanyId = cmd.CompanyId,
            AccountingPeriodId = entry.AccountingPeriodId,
            EntryType = EntryType.Correction,
            EntryNumber = reversalEntryNumber,
            EntryDate = DateOnly.FromDateTime(now.UtcDateTime),
            Description = $"İPTAL: {entry.EntryNumber} — {cmd.VoidReason}",
            Reference = entry.Reference,
            ReferenceId = entry.ReferenceId,
            TotalDebit = entry.TotalCredit,   // tersine çevrildi
            TotalCredit = entry.TotalDebit,   // tersine çevrildi
            Status = JournalEntryStatus.Posted,
            PostedAt = now,
            PostedBy = session.UserId,
            OriginalEntryId = entry.Id,
            Lines = entry.Lines.Select(l => new JournalLine
            {
                TenantId = cmd.TenantId,
                CompanyId = cmd.CompanyId,
                AccountCodeId = l.AccountCodeId,
                AccountCodeValue = l.AccountCodeValue,
                Debit = l.Credit,             // tersine çevrildi
                Credit = l.Debit,             // tersine çevrildi
                Description = l.Description,
                CostCenterId = l.CostCenterId,
                TaxCode = l.TaxCode,
                UnitId = l.UnitId,
                OriginalAmount = l.OriginalAmount,
                OriginalCurrency = l.OriginalCurrency,
                ExchangeRate = l.ExchangeRate
            }).ToList()
        };

        _db.JournalEntries.Add(reversalEntry);

        // 6. Orijinal fişi Voided durumuna taşı
        entry.Status = JournalEntryStatus.Voided;
        entry.VoidedAt = now;
        entry.VoidedBy = session.UserId;
        entry.VoidReason = cmd.VoidReason;

        // 7. Tek transaction'da persist
        await _db.SaveChangesAsync(cancellationToken);

        return Result<JournalEntryCreated>.Success(
            new JournalEntryCreated(reversalEntry.Id, reversalEntry.EntryNumber));
    }
}
