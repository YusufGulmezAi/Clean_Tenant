using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Collections;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Collections.RefundAdvance;

/// <summary>
/// <see cref="RefundAdvanceCommand"/> handler — avans bakiyesinden nakit iade +
/// yevmiye (Borç 120 / Alacak Kasa-Banka). Onay ayarına göre Posted/PendingApproval.
/// </summary>
public sealed class RefundAdvanceCommandHandler
    : IRequestHandler<RefundAdvanceCommand, Result<RefundResult>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RefundAdvanceCommandHandler(IMainDbContext db, IClock clock, ICurrentSessionAccessor session)
    {
        _db = db;
        _clock = clock;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<RefundResult>> Handle(
        RefundAdvanceCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0m)
            return Result<RefundResult>.Failure(Error.Failure("REF-001", "İade tutarı sıfırdan büyük olmalı."));

        // Avans kaynakları (en eski önce)
        var advances = await _db.Collections
            .Where(c => c.UnitId == request.UnitId && c.CompanyId == request.CompanyId
                     && !c.IsDeleted && c.UnallocatedAmount > 0m)
            .OrderBy(c => c.PaymentDate).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        var totalAdvance = advances.Sum(c => c.UnallocatedAmount);
        if (request.Amount > totalAdvance)
            return Result<RefundResult>.Failure(
                Error.Failure("REF-002", $"İade tutarı avans bakiyesini aşıyor (avans: {totalAdvance:N2})."));

        // Nakit çıkış hesabı geçerli mi (yaprak, aktif)
        var cash = await _db.AccountCodes
            .FirstOrDefaultAsync(a => a.Id == request.CashAccountCodeId
                                   && a.CompanyId == request.CompanyId
                                   && a.IsActive && a.IsDetail && !a.IsDeleted, cancellationToken);
        if (cash is null)
            return Result<RefundResult>.Failure(
                Error.Failure("REF-003", "Geçersiz/pasif/özet kasa-banka hesabı (yaprak gerekir)."));

        // Avansın durduğu 120 hesabı = BB'nin en yeni tahakkuğunun alacak hesabı
        var advanceAccountId = await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where d.UnitId == request.UnitId && a.CompanyId == request.CompanyId
                && !d.IsDeleted && !a.IsDeleted && a.ReceivableAccountCodeId != null
            orderby a.Year descending, a.Month descending
            select a.ReceivableAccountCodeId).FirstOrDefaultAsync(cancellationToken);
        if (advanceAccountId is not { } advId)
            return Result<RefundResult>.Failure(
                Error.Failure("REF-004", "İade için alacak (avans) hesabı belirlenemedi."));

        var advAccount = await _db.AccountCodes
            .FirstOrDefaultAsync(a => a.Id == advId && !a.IsDeleted, cancellationToken);
        if (advAccount is null)
            return Result<RefundResult>.Failure(
                Error.Failure("REF-004", "İade için alacak (avans) hesabı bulunamadı."));

        // Yevmiyenin işleneceği açık dönem (RefundDate'i içeren, yoksa en güncel açık)
        var openPeriods = await _db.AccountingPeriods
            .Include(p => p.FiscalYear)
            .Where(p => p.CompanyId == request.CompanyId && !p.IsDeleted && p.Status == PeriodStatus.Open)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .ToListAsync(cancellationToken);
        var period = openPeriods.FirstOrDefault(p => request.RefundDate >= p.StartDate && request.RefundDate <= p.EndDate)
                     ?? openPeriods.FirstOrDefault();
        if (period is null)
            return Result<RefundResult>.Failure(Error.Failure("REF-005", "Açık muhasebe dönemi yok."));

        var settings = await _db.AccountingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId && !s.IsDeleted, cancellationToken);
        var requireApproval = settings?.RequireApproval ?? false;

        var now = _clock.UtcNow;
        var userId = _session.Current?.UserId;

        // Avans bakiyesinden düş (en eski önce)
        var remaining = request.Amount;
        foreach (var adv in advances)
        {
            if (remaining <= 0m) break;
            var take = Math.Min(remaining, adv.UnallocatedAmount);
            adv.UnallocatedAmount -= take;
            remaining -= take;
        }

        var refund = new CollectionRefund
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            UnitId = request.UnitId,
            Amount = request.Amount,
            RefundDate = request.RefundDate,
            CashAccountCodeId = cash.Id,
            AdvanceAccountCodeId = advId,
            Method = request.Method,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            RefundedAt = now,
            RefundedBy = userId,
        };

        // Yevmiye fişi: Borç 120 (avans) / Alacak 100-102 (kasa). EntryType.Normal —
        // numara serisi (company, fiscalYear, Normal) paylaşılır → (company, entry_number)
        // benzersizliği korunur (tüm nakit fişleri tek seri kullanır).
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

        var desc = refund.Description ?? $"Avans iadesi — BB {request.UnitId:N}";
        var status = requireApproval ? JournalEntryStatus.PendingApproval : JournalEntryStatus.Posted;
        var entry = new JournalEntry
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            AccountingPeriodId = period.Id,
            EntryType = EntryType.Normal,
            EntryNumber = entryNumber,
            EntryDate = request.RefundDate,
            Description = desc,
            Reference = "REFUND",
            ReferenceId = refund.Id,
            TotalDebit = request.Amount,
            TotalCredit = request.Amount,
            Status = status,
            PostedAt = status == JournalEntryStatus.Posted ? now : null,
            PostedBy = status == JournalEntryStatus.Posted ? userId : null,
        };
        // Borç: 120 (avansın durduğu alacak hesabı) — alacaklı bakiyeyi kapatır
        entry.Lines.Add(new JournalLine
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            AccountCodeId = advId,
            AccountCodeValue = advAccount.Code,
            Debit = request.Amount,
            Credit = 0m,
            Description = desc,
        });
        // Alacak: Kasa/Banka (nakit çıkış)
        entry.Lines.Add(new JournalLine
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            AccountCodeId = cash.Id,
            AccountCodeValue = cash.Code,
            Debit = 0m,
            Credit = request.Amount,
            Description = desc,
        });

        _db.JournalEntries.Add(entry);
        refund.JournalEntryId = entry.Id;
        _db.CollectionRefunds.Add(refund);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<RefundResult>.Success(new RefundResult(
            refund.Id, request.Amount, totalAdvance - request.Amount, requireApproval));
    }
}
