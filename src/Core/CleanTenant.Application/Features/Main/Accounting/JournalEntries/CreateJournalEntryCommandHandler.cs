using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="CreateJournalEntryCommand"/> handler.
/// <para>
/// İş kuralları (sırasıyla):
/// <list type="number">
///   <item>Muhasebe dönemi varlık ve açıklık kontrolü</item>
///   <item>Satır sayısı (min 2)</item>
///   <item>Borç-alacak dengesi</item>
///   <item>Her satır: debit XOR credit</item>
///   <item>AccountCode: şirkete ait, aktif, yaprak (IsDetail=true)</item>
///   <item>CostCenter: şirkete ait, aktif (varsa)</item>
///   <item>EntrySequence atomik artış → EntryNumber üretimi</item>
///   <item>JournalEntry + Lines kayıt</item>
/// </list>
/// </para>
/// </summary>
public sealed class CreateJournalEntryCommandHandler
    : IRequestHandler<CreateJournalEntryCommand, Result<JournalEntryCreated>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateJournalEntryCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<JournalEntryCreated>> Handle(
        CreateJournalEntryCommand cmd,
        CancellationToken cancellationToken)
    {
        // 1. Muhasebe dönemi kontrolü
        var period = await _db.AccountingPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(
                p => p.Id == cmd.AccountingPeriodId
                  && p.CompanyId == cmd.CompanyId, cancellationToken);

        if (period is null)
            return Result<JournalEntryCreated>.Failure(
                Error.NotFound("ACC-004", "Muhasebe dönemi bulunamadı."));

        if (period.Status != PeriodStatus.Open)
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-201", "Muhasebe dönemi açık değil."));

        // 2. Satır sayısı kontrolü
        if (cmd.Lines.Count < 2)
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-101", "Yevmiye fişi en az 2 satır içermelidir."));

        // 3. Borç-alacak dengesi kontrolü
        var totalDebit = cmd.Lines.Sum(l => l.Debit);
        var totalCredit = cmd.Lines.Sum(l => l.Credit);
        if (totalDebit != totalCredit || totalDebit <= 0)
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-103", "Borç ve alacak toplamları eşit ve sıfırdan büyük olmalıdır."));

        // 4. Her satır: debit XOR credit
        if (cmd.Lines.Any(l => (l.Debit == 0 && l.Credit == 0) || (l.Debit > 0 && l.Credit > 0)))
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-105", "Her satır yalnızca borç veya alacak içerebilir."));

        // 5. AccountCode validasyonu (toplu çek, N+1 önleme)
        var accountCodeIds = cmd.Lines.Select(l => l.AccountCodeId).Distinct().ToList();
        var accountCodes = await _db.AccountCodes
            .Where(ac => accountCodeIds.Contains(ac.Id)
                      && ac.CompanyId == cmd.CompanyId
                      && !ac.IsDeleted)
            .Select(ac => new { ac.Id, ac.Code, ac.IsActive, ac.IsDetail })
            .ToListAsync(cancellationToken);

        if (accountCodes.Count != accountCodeIds.Count)
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-107", "Bir veya daha fazla hesap kodu bu şirkete ait değil."));

        if (accountCodes.Any(ac => !ac.IsActive))
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-108", "Pasif hesap koduna fiş girilemez."));

        if (accountCodes.Any(ac => !ac.IsDetail))
            return Result<JournalEntryCreated>.Failure(
                Error.Failure("ACC-109", "Yalnızca yaprak hesaplara (IsDetail=true) fiş girilebilir."));

        // 6. CostCenter validasyonu (varsa)
        var costCenterIds = cmd.Lines
            .Where(l => l.CostCenterId.HasValue)
            .Select(l => l.CostCenterId!.Value)
            .Distinct()
            .ToList();

        if (costCenterIds.Count > 0)
        {
            var validCostCenterCount = await _db.CostCenters
                .CountAsync(cc => costCenterIds.Contains(cc.Id)
                               && cc.CompanyId == cmd.CompanyId
                               && cc.IsActive
                               && !cc.IsDeleted, cancellationToken);

            if (validCostCenterCount != costCenterIds.Count)
                return Result<JournalEntryCreated>.Failure(
                    Error.Failure("ACC-110", "Geçersiz veya pasif maliyet merkezi."));
        }

        // 7. EntrySequence atomik artış
        var sequence = await _db.EntrySequences
            .FirstOrDefaultAsync(
                es => es.CompanyId == cmd.CompanyId
                   && es.FiscalYearId == period.FiscalYearId
                   && es.EntryType == cmd.EntryType, cancellationToken);

        if (sequence is null)
        {
            sequence = new EntrySequence
            {
                TenantId = cmd.TenantId,
                CompanyId = cmd.CompanyId,
                FiscalYearId = period.FiscalYearId,
                EntryType = cmd.EntryType,
                LastNumber = 0
            };
            _db.EntrySequences.Add(sequence);
        }

        sequence.LastNumber++;
        var entryNumber = $"{period.FiscalYear.Label}/{sequence.LastNumber:D6}";

        // 8. AccountCode değerlerini map'le (denormalize)
        var accountCodeMap = accountCodes.ToDictionary(ac => ac.Id, ac => ac.Code);

        // 9. JournalEntry + Lines oluştur
        var entry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            CompanyId = cmd.CompanyId,
            AccountingPeriodId = cmd.AccountingPeriodId,
            EntryType = cmd.EntryType,
            EntryNumber = entryNumber,
            EntryDate = cmd.EntryDate,
            Description = cmd.Description,
            Reference = cmd.Reference,
            ReferenceId = cmd.ReferenceId,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            Status = JournalEntryStatus.Draft,
            Lines = cmd.Lines.Select(l => new JournalLine
            {
                TenantId = cmd.TenantId,
                CompanyId = cmd.CompanyId,
                AccountCodeId = l.AccountCodeId,
                AccountCodeValue = accountCodeMap[l.AccountCodeId],
                Debit = l.Debit,
                Credit = l.Credit,
                Description = l.Description,
                CostCenterId = l.CostCenterId,
                TaxCode = l.TaxCode,
                UnitId = l.UnitId,
                OriginalAmount = l.OriginalAmount,
                OriginalCurrency = l.OriginalCurrency,
                ExchangeRate = l.ExchangeRate
            }).ToList()
        };

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<JournalEntryCreated>.Success(
            new JournalEntryCreated(entry.Id, entry.EntryNumber));
    }
}
