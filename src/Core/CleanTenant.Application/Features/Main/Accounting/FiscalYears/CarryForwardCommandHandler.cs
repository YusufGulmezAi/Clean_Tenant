using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>
/// <see cref="CarryForwardCommand"/> handler.
/// <para>
/// Kapalı mali yılın bilanço hesaplarının (sınıf 1–5) bakiyelerini hesaplar ve
/// yeni mali yılın ilk açık dönemine Opening tipi fiş olarak yazar.
/// EntrySequence sayacı artırılır.
/// </para>
/// </summary>
public sealed class CarryForwardCommandHandler
    : IRequestHandler<CarryForwardCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CarryForwardCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CarryForwardCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Kapatılacak mali yılı kontrol et
        var closedYear = await _db.FiscalYears
            .FirstOrDefaultAsync(fy => fy.Id == command.FiscalYearId
                                    && fy.CompanyId == command.CompanyId
                                    && !fy.IsDeleted, cancellationToken);

        if (closedYear is null)
            return Result<Guid>.Failure(
                Error.NotFound("ACC-003", "Mali yıl bulunamadı."));

        if (closedYear.Status != PeriodStatus.ClosedPermanent)
            return Result<Guid>.Failure(
                Error.Failure("ACC-213", "Devir yalnızca kalıcı kapalı mali yıl için yapılabilir."));

        // 2. Yeni mali yılı bul (StartDate = kapalı yıl EndDate + 1 gün)
        var newYearStartDate = closedYear.EndDate.AddDays(1);
        var newFiscalYear = await _db.FiscalYears
            .Include(fy => fy.Periods.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(fy => fy.CompanyId == command.CompanyId
                                    && fy.StartDate == newYearStartDate
                                    && !fy.IsDeleted, cancellationToken);

        if (newFiscalYear is null)
            return Result<Guid>.Failure(
                Error.NotFound("ACC-214", "Devir yapılacak yeni mali yıl bulunamadı."));

        var firstPeriod = newFiscalYear.Periods
            .OrderBy(p => p.StartDate)
            .FirstOrDefault(p => p.Status == PeriodStatus.Open);

        if (firstPeriod is null)
            return Result<Guid>.Failure(
                Error.Failure("ACC-201", "Yeni mali yılda açık muhasebe dönemi bulunamadı."));

        // 3. Bilanço sınıflarını listele (sınıf 1–5)
        var balanceSheetClasses = new[]
        {
            AccountClass.CurrentAsset,
            AccountClass.NonCurrentAsset,
            AccountClass.ShortTermLiability,
            AccountClass.LongTermLiability,
            AccountClass.Equity
        };

        // Kapalı mali yıldaki period id'lerini al
        var closedPeriodIds = await _db.AccountingPeriods
            .Where(p => p.FiscalYearId == command.FiscalYearId
                     && p.CompanyId == command.CompanyId
                     && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        // Bilanço hesap kodu id'lerini al (şirkete ait, aktif)
        var balanceSheetAccountIds = await _db.AccountCodes
            .Where(ac => ac.CompanyId == command.CompanyId
                      && !ac.IsDeleted
                      && ac.IsActive
                      && balanceSheetClasses.Contains(ac.AccountClass))
            .Select(ac => ac.Id)
            .ToListAsync(cancellationToken);

        if (balanceSheetAccountIds.Count == 0)
            return Result<Guid>.Failure(
                Error.Failure("ACC-218", "Bilanço hesapları bulunamadı."));

        // Fiş satırı bakiyelerini hesapla (accounting_period → posted entries → lines)
        var lineBalances = await _db.JournalLines
            .Where(jl => jl.CompanyId == command.CompanyId
                      && !jl.IsDeleted
                      && balanceSheetAccountIds.Contains(jl.AccountCodeId)
                      && closedPeriodIds.Contains(jl.JournalEntry.AccountingPeriodId)
                      && jl.JournalEntry.Status == JournalEntryStatus.Posted)
            .GroupBy(jl => new { jl.AccountCodeId, jl.AccountCodeValue })
            .Select(g => new
            {
                g.Key.AccountCodeId,
                g.Key.AccountCodeValue,
                Balance = g.Sum(jl => jl.Debit) - g.Sum(jl => jl.Credit)
            })
            .Where(x => x.Balance != 0)
            .ToListAsync(cancellationToken);

        if (lineBalances.Count == 0)
            return Result<Guid>.Failure(
                Error.Failure("ACC-215", "Devir için bilanço bakiyesi bulunamadı."));

        // 4. EntrySequence sayacını artır
        var sequence = await _db.EntrySequences
            .FirstOrDefaultAsync(es => es.CompanyId == command.CompanyId
                                    && es.FiscalYearId == newFiscalYear.Id
                                    && es.EntryType == EntryType.Opening
                                    && !es.IsDeleted, cancellationToken);

        if (sequence is null)
        {
            sequence = new EntrySequence
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                FiscalYearId = newFiscalYear.Id,
                EntryType = EntryType.Opening,
                LastNumber = 0
            };
            _db.EntrySequences.Add(sequence);
        }

        sequence.LastNumber++;
        var entryNumber = $"{newFiscalYear.Label}/{sequence.LastNumber:D6}";

        // 5. Açılış fişini oluştur
        var openingEntry = new JournalEntry
        {
            TenantId = command.TenantId,
            CompanyId = command.CompanyId,
            AccountingPeriodId = firstPeriod.Id,
            EntryType = EntryType.Opening,
            EntryNumber = entryNumber,
            EntryDate = newFiscalYear.StartDate,
            Description = $"{closedYear.Label} → {newFiscalYear.Label} açılış devri",
            Status = JournalEntryStatus.Posted,
            PostedAt = DateTimeOffset.UtcNow
        };

        // 6. Açılış fişi satırlarını oluştur
        var lines = new List<JournalLine>();
        foreach (var balance in lineBalances)
        {
            var line = new JournalLine
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                JournalEntry = openingEntry,
                AccountCodeId = balance.AccountCodeId,
                AccountCodeValue = balance.AccountCodeValue,
                // Borç bakiyesi > 0 → borç tarafa, < 0 → alacak tarafa
                Debit = balance.Balance > 0 ? balance.Balance : 0,
                Credit = balance.Balance < 0 ? Math.Abs(balance.Balance) : 0,
                Description = "Açılış devri"
            };
            lines.Add(line);
        }

        openingEntry.Lines = lines;
        openingEntry.TotalDebit = lines.Sum(l => l.Debit);
        openingEntry.TotalCredit = lines.Sum(l => l.Credit);

        _db.JournalEntries.Add(openingEntry);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(openingEntry.Id);
    }
}
