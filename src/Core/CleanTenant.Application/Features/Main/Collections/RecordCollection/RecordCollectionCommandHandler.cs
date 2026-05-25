using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.Domain.Tenant.Collections;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Collections.RecordCollection;

/// <summary>
/// <see cref="RecordCollectionCommand"/> handler — tahsilat + TBK m.101 dağıtım +
/// otomatik yevmiye fişi (Kasa/Banka borç / 120 alacak gruplu).
/// </summary>
public sealed class RecordCollectionCommandHandler
    : IRequestHandler<RecordCollectionCommand, Result<CollectionResult>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RecordCollectionCommandHandler(IMainDbContext db, IClock clock, ICurrentSessionAccessor session)
    {
        _db = db;
        _clock = clock;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<CollectionResult>> Handle(
        RecordCollectionCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0m)
            return Result<CollectionResult>.Failure(Error.Failure("COL-001", "Tutar sıfırdan büyük olmalı."));

        // Dönem + mali yıl
        var period = await _db.AccountingPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.Id == request.AccountingPeriodId
                                   && p.CompanyId == request.CompanyId
                                   && !p.IsDeleted, cancellationToken);
        if (period is null)
            return Result<CollectionResult>.Failure(Error.NotFound("COL-002", "Muhasebe dönemi bulunamadı."));
        if (period.Status != PeriodStatus.Open)
            return Result<CollectionResult>.Failure(Error.Failure("COL-003", "Kapalı döneme tahsilat girilemez."));

        // Kasa/banka hesabı geçerli mi
        var cash = await _db.AccountCodes
            .FirstOrDefaultAsync(a => a.Id == request.CashAccountCodeId
                                   && a.CompanyId == request.CompanyId
                                   && a.IsActive && a.IsDetail && !a.IsDeleted, cancellationToken);
        if (cash is null)
            return Result<CollectionResult>.Failure(
                Error.Failure("COL-004", "Geçersiz/pasif/özet kasa-banka hesabı (yaprak gerekir)."));

        // BB'nin açık tahakkuk detayları (en eski vade önce)
        var details = await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where d.UnitId == request.UnitId && a.CompanyId == request.CompanyId
                && !d.IsDeleted && !a.IsDeleted
            select new { d.Id, d.Amount, d.DueDate, a.ReceivableAccountCodeId, a.Source }
        ).ToListAsync(cancellationToken);

        var detailIds = details.Select(x => x.Id).ToList();
        var allocatedMap = (await _db.CollectionAllocations
            .Where(al => detailIds.Contains(al.AccrualDetailId) && !al.IsDeleted)
            .GroupBy(al => al.AccrualDetailId)
            .Select(g => new { DetailId = g.Key, Sum = g.Sum(x => x.AllocatedAmount) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DetailId, x => x.Sum);

        var open = details
            .Select(d => new
            {
                d.Id,
                d.ReceivableAccountCodeId,
                d.DueDate,
                d.Source,
                Remaining = d.Amount - allocatedMap.GetValueOrDefault(d.Id, 0m),
            })
            .Where(d => d.Remaining > 0m)
            // TBK m.101: en eski vade içinde önce gecikme faizi, sonra anapara
            .OrderBy(d => d.DueDate)
            .ThenByDescending(d => d.Source == AccrualSource.LateFee)
            .ThenBy(d => d.Id)
            .ToList();

        var totalOpen = open.Sum(d => d.Remaining);
        if (totalOpen <= 0m)
            return Result<CollectionResult>.Failure(Error.Failure("COL-006", "BB'nin açık borcu yok."));
        // Fazla ödeme (Amount > totalOpen) artık reddedilmez (eski COL-005 kaldırıldı):
        // fazlalık AVANS olarak BB'nin alacak hesabına kredilenir → 120 bakiyesi
        // alacaklıya (negatif) döner. Ayrı 340 Alınan Avanslar hesabı kullanılmaz.

        // Tahsilat + TBK m.101 dağıtım
        var now = _clock.UtcNow;
        var userId = _session.Current?.UserId;
        var collection = new Collection
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            UnitId = request.UnitId,
            AccountingPeriodId = period.Id,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            Method = request.Method,
            CashAccountCodeId = request.CashAccountCodeId,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            UnallocatedAmount = 0m,
            RecordedAt = now,
            RecordedBy = userId,
        };

        var remaining = request.Amount;
        var creditByAccount = new Dictionary<Guid, decimal>();
        foreach (var d in open)
        {
            if (remaining <= 0m) break;
            if (d.ReceivableAccountCodeId is not { } recvId)
                return Result<CollectionResult>.Failure(
                    Error.Failure("COL-007", "Açık tahakkukta alacak hesabı yok; tahsilat yevmiyesi açılamaz."));

            var apply = Math.Min(remaining, d.Remaining);
            collection.Allocations.Add(new CollectionAllocation
            {
                TenantId = request.TenantId,
                CollectionId = collection.Id,
                AccrualDetailId = d.Id,
                AllocatedAmount = apply,
            });
            creditByAccount[recvId] = creditByAccount.GetValueOrDefault(recvId) + apply;
            remaining -= apply;
        }

        // Fazla ödeme → avans: kalan tutar BB'nin (en yeni açık tahakkuğun) alacak
        // hesabına kredilenir; 120 bakiyesi alacaklıya (negatif) döner. UnallocatedAmount
        // avansı izler; sonradan mahsup (Slice 2) veya iade (Slice 3) edilir.
        if (remaining > 0m)
        {
            var advanceAccountId = open[^1].ReceivableAccountCodeId ?? open[0].ReceivableAccountCodeId;
            if (advanceAccountId is not { } advId)
                return Result<CollectionResult>.Failure(
                    Error.Failure("COL-007", "Avans için alacak hesabı belirlenemedi."));
            creditByAccount[advId] = creditByAccount.GetValueOrDefault(advId) + remaining;
            collection.UnallocatedAmount = remaining;
        }

        // Yevmiye fişi: Borç Kasa/Banka / Alacak 120.0X.NNN (hesap bazında gruplu)
        var recvCodeIds = creditByAccount.Keys.ToList();
        var recvCodes = await _db.AccountCodes
            .Where(a => recvCodeIds.Contains(a.Id) && !a.IsDeleted)
            .Select(a => new { a.Id, a.Code })
            .ToListAsync(cancellationToken);
        var recvCodeMap = recvCodes.ToDictionary(x => x.Id, x => x.Code);

        var sequence = await _db.EntrySequences
            .FirstOrDefaultAsync(es => es.CompanyId == request.CompanyId
                                    && es.FiscalYearId == period.FiscalYearId
                                    && es.EntryType == EntryType.Normal, cancellationToken);
        if (sequence is null)
        {
            sequence = new EntrySequence
            {
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                FiscalYearId = period.FiscalYearId,
                EntryType = EntryType.Normal,
                LastNumber = 0,
            };
            _db.EntrySequences.Add(sequence);
        }
        sequence.LastNumber++;
        var entryNumber = $"{period.FiscalYear.Label}/{sequence.LastNumber:D6}";

        var desc = collection.Description ?? $"Tahsilat — BB {request.UnitId:N}";
        var entry = new JournalEntry
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            AccountingPeriodId = period.Id,
            EntryType = EntryType.Normal,
            EntryNumber = entryNumber,
            EntryDate = request.PaymentDate,
            Description = desc,
            Reference = "COLLECTION",
            ReferenceId = collection.Id,
            TotalDebit = request.Amount,
            TotalCredit = request.Amount,
            Status = JournalEntryStatus.Posted,
            PostedAt = now,
            PostedBy = userId,
        };
        // Borç: Kasa/Banka (toplam)
        entry.Lines.Add(new JournalLine
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            AccountCodeId = cash.Id,
            AccountCodeValue = cash.Code,
            Debit = request.Amount,
            Credit = 0m,
            Description = desc,
        });
        // Alacak: her 120 hesabı için ayrı satır
        foreach (var (accId, sum) in creditByAccount)
        {
            entry.Lines.Add(new JournalLine
            {
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                AccountCodeId = accId,
                AccountCodeValue = recvCodeMap.GetValueOrDefault(accId, "?"),
                Debit = 0m,
                Credit = sum,
                Description = desc,
            });
        }

        _db.JournalEntries.Add(entry);
        collection.JournalEntryId = entry.Id;
        _db.Collections.Add(collection);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<CollectionResult>.Success(new CollectionResult(
            collection.Id, request.Amount, collection.UnallocatedAmount, collection.Allocations.Count));
    }
}
