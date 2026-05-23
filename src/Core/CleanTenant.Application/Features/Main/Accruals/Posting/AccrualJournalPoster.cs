using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accruals.Posting;

/// <summary>
/// <see cref="IAccrualJournalPoster"/> implementasyonu. Saf Application servisi
/// (yalnız <see cref="IMainDbContext"/> + saat + oturum). Posted fiş üretir.
/// </summary>
public sealed class AccrualJournalPoster : IAccrualJournalPoster
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AccrualJournalPoster(IMainDbContext db, IClock clock, ICurrentSessionAccessor session)
    {
        _db = db;
        _clock = clock;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> PostAsync(Accrual accrual, CancellationToken cancellationToken)
    {
        if (accrual.ReceivableAccountCodeId is not { } receivableId
            || accrual.IncomeAccountCodeId is not { } incomeId)
            return Result<Guid>.Failure(
                Error.Failure("ACR-301", "Tahakkuk hesap kodları eksik; fiş açılamaz."));

        if (accrual.AccountingPeriodId is not { } periodId)
            return Result<Guid>.Failure(
                Error.Failure("ACR-302", "Tahakkuk muhasebe dönemi yok; fiş açılamaz."));

        // Dönem + mali yıl
        var period = await _db.AccountingPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.Id == periodId && !p.IsDeleted, cancellationToken);
        if (period is null)
            return Result<Guid>.Failure(Error.NotFound("ACR-303", "Muhasebe dönemi bulunamadı."));
        if (period.Status != PeriodStatus.Open)
            return Result<Guid>.Failure(Error.Failure("ACR-304", "Kapalı/kilitli döneme fiş açılamaz."));

        // Hesap kodlarının denormalize string değerleri (yevmiye satırı için)
        var codes = await _db.AccountCodes
            .Where(a => (a.Id == receivableId || a.Id == incomeId) && !a.IsDeleted)
            .Select(a => new { a.Id, a.Code })
            .ToListAsync(cancellationToken);
        var receivableCode = codes.FirstOrDefault(c => c.Id == receivableId)?.Code;
        var incomeCode = codes.FirstOrDefault(c => c.Id == incomeId)?.Code;
        if (receivableCode is null || incomeCode is null)
            return Result<Guid>.Failure(Error.NotFound("ACR-305", "Hesap kodu kaydı bulunamadı."));

        // Fiş numarası — EntrySequence (Normal tip)
        var sequence = await _db.EntrySequences
            .FirstOrDefaultAsync(es => es.CompanyId == accrual.CompanyId
                                    && es.FiscalYearId == period.FiscalYearId
                                    && es.EntryType == EntryType.Normal, cancellationToken);
        if (sequence is null)
        {
            sequence = new EntrySequence
            {
                TenantId = accrual.TenantId,
                CompanyId = accrual.CompanyId,
                FiscalYearId = period.FiscalYearId,
                EntryType = EntryType.Normal,
                LastNumber = 0,
            };
            _db.EntrySequences.Add(sequence);
        }
        sequence.LastNumber++;
        var entryNumber = $"{period.FiscalYear.Label}/{sequence.LastNumber:D6}";

        var now = _clock.UtcNow;
        var userId = _session.Current?.UserId;

        // Fiş: Borç 120.0X.NNN / Alacak 600.0X.NNN (toplam)
        var entry = new JournalEntry
        {
            TenantId = accrual.TenantId,
            CompanyId = accrual.CompanyId,
            AccountingPeriodId = periodId,
            EntryType = EntryType.Normal,
            EntryNumber = entryNumber,
            EntryDate = period.EndDate,
            Description = accrual.Description,
            Reference = "ACCRUAL",
            ReferenceId = accrual.Id,
            TotalDebit = accrual.TotalAmount,
            TotalCredit = accrual.TotalAmount,
            Status = JournalEntryStatus.Posted,
            PostedAt = now,
            PostedBy = userId,
            Lines =
            [
                new JournalLine
                {
                    TenantId = accrual.TenantId,
                    CompanyId = accrual.CompanyId,
                    AccountCodeId = receivableId,
                    AccountCodeValue = receivableCode,
                    Debit = accrual.TotalAmount,
                    Credit = 0m,
                    Description = accrual.Description,
                },
                new JournalLine
                {
                    TenantId = accrual.TenantId,
                    CompanyId = accrual.CompanyId,
                    AccountCodeId = incomeId,
                    AccountCodeValue = incomeCode,
                    Debit = 0m,
                    Credit = accrual.TotalAmount,
                    Description = accrual.Description,
                },
            ],
        };

        _db.JournalEntries.Add(entry);
        accrual.JournalEntryId = entry.Id;

        return Result<Guid>.Success(entry.Id);
    }
}
