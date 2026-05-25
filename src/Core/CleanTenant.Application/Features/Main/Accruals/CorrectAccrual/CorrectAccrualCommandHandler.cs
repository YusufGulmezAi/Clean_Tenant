using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accruals.CorrectAccrual;

/// <summary>
/// <see cref="CorrectAccrualCommand"/> handler — Correction tahakkuğu (negatif detay) +
/// ters yönlü dengeli yevmiye. Onay ayarına göre Posted/PendingApproval.
/// </summary>
public sealed class CorrectAccrualCommandHandler
    : IRequestHandler<CorrectAccrualCommand, Result<CorrectionResult>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CorrectAccrualCommandHandler(IMainDbContext db, IClock clock, ICurrentSessionAccessor session)
    {
        _db = db;
        _clock = clock;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<CorrectionResult>> Handle(
        CorrectAccrualCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0m)
            return Result<CorrectionResult>.Failure(Error.Failure("COR-001", "Düzeltme tutarı sıfırdan büyük olmalı."));

        // Orijinal detay + tahakkuk
        var original = await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where d.Id == request.AccrualDetailId && a.CompanyId == request.CompanyId
                && !d.IsDeleted && !a.IsDeleted
            select new { Detail = d, Accrual = a }).FirstOrDefaultAsync(cancellationToken);
        if (original is null)
            return Result<CorrectionResult>.Failure(Error.NotFound("COR-002", "Tahakkuk detayı bulunamadı."));
        if (original.Accrual.Source == AccrualSource.Correction)
            return Result<CorrectionResult>.Failure(Error.Failure("COR-003", "Düzeltme kaydı tekrar düzeltilemez."));
        if (original.Accrual.ReceivableAccountCodeId is not { } recvId
            || original.Accrual.IncomeAccountCodeId is not { } incId)
            return Result<CorrectionResult>.Failure(
                Error.Failure("COR-004", "Orijinal tahakkukta hesap kodları eksik; ters kayıt açılamaz."));

        // Aşırı-ters-kayıt kontrolü: orijinalin kalan (düzeltilmemiş) tutarı
        var correctionSumNeg = await _db.AccrualDetails
            .Where(d => d.CorrectedAccrualDetailId == request.AccrualDetailId && !d.IsDeleted)
            .SumAsync(d => d.Amount, cancellationToken); // negatif toplam
        var alreadyCorrected = -correctionSumNeg;
        var netCharge = original.Detail.Amount - alreadyCorrected;
        if (request.Amount > netCharge)
            return Result<CorrectionResult>.Failure(
                Error.Failure("COR-005", $"Düzeltme tutarı kalan tahakkuğu aşıyor (kalan: {netCharge:N2})."));

        // Açık dönem (en güncel açık) — ters kayıt buraya işlenir
        var openPeriods = await _db.AccountingPeriods
            .Include(p => p.FiscalYear)
            .Where(p => p.CompanyId == request.CompanyId && !p.IsDeleted && p.Status == PeriodStatus.Open)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .ToListAsync(cancellationToken);
        var period = openPeriods.FirstOrDefault();
        if (period is null)
            return Result<CorrectionResult>.Failure(Error.Failure("COR-006", "Açık muhasebe dönemi yok."));

        // Hesap kodu string değerleri
        var codes = await _db.AccountCodes
            .Where(a => (a.Id == recvId || a.Id == incId) && !a.IsDeleted)
            .Select(a => new { a.Id, a.Code })
            .ToListAsync(cancellationToken);
        var recvCode = codes.FirstOrDefault(c => c.Id == recvId)?.Code;
        var incCode = codes.FirstOrDefault(c => c.Id == incId)?.Code;
        if (recvCode is null || incCode is null)
            return Result<CorrectionResult>.Failure(Error.NotFound("COR-007", "Hesap kodu kaydı bulunamadı."));

        var settings = await _db.AccountingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId && !s.IsDeleted, cancellationToken);
        var requireApproval = settings?.RequireApproval ?? false;

        var now = _clock.UtcNow;
        var userId = _session.Current?.UserId;
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Düzeltme" : request.Reason.Trim();
        var desc = $"Düzeltme ({reason}) — BB {original.Detail.UnitId:N}";

        // Correction tahakkuğu — NEGATİF tutarlı detay (orijinale bağlı)
        var correctionAccrual = new Accrual
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            Source = AccrualSource.Correction,
            AccountingPeriodId = period.Id,
            Year = period.Year,
            Month = period.Month,
            TotalAmount = -request.Amount,
            ReceivableAccountCodeId = recvId,
            IncomeAccountCodeId = incId,
            Description = desc,
            GeneratedAt = now,
            GeneratedBy = userId,
            ResponsibilityMode = original.Accrual.ResponsibilityMode,
        };
        var correctionDetail = new AccrualDetail
        {
            TenantId = request.TenantId,
            AccrualId = correctionAccrual.Id,
            UnitId = original.Detail.UnitId,
            Amount = -request.Amount,
            DistributionShare = 0m,
            DueDate = original.Detail.DueDate, // aynı vade → overdue net olarak doğru
            PrimaryResponsiblePartyId = original.Detail.PrimaryResponsiblePartyId,
            ResponsibleResolvedNote = "Düzeltme (ters kayıt)",
            CorrectedAccrualDetailId = original.Detail.Id,
        };
        correctionAccrual.Details.Add(correctionDetail);
        _db.Accruals.Add(correctionAccrual);

        // Ters yönlü yevmiye: Borç 600 (gelir) / Alacak 120 (alacak) — pozitif tutar.
        // Numara serisi Normal (paylaşımlı → (company, entry_number) benzersiz).
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

        var status = requireApproval ? JournalEntryStatus.PendingApproval : JournalEntryStatus.Posted;
        var entry = new JournalEntry
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            AccountingPeriodId = period.Id,
            EntryType = EntryType.Correction,
            EntryNumber = entryNumber,
            EntryDate = period.EndDate,
            Description = desc,
            Reference = "CORRECTION",
            ReferenceId = correctionAccrual.Id,
            OriginalEntryId = original.Accrual.JournalEntryId,
            TotalDebit = request.Amount,
            TotalCredit = request.Amount,
            Status = status,
            PostedAt = status == JournalEntryStatus.Posted ? now : null,
            PostedBy = status == JournalEntryStatus.Posted ? userId : null,
        };
        // Borç: gelir hesabı (orijinal alacağın gelirini geri alır)
        entry.Lines.Add(new JournalLine
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            AccountCodeId = incId,
            AccountCodeValue = incCode,
            Debit = request.Amount,
            Credit = 0m,
            Description = desc,
        });
        // Alacak: alacak hesabı (orijinal borcu geri alır)
        entry.Lines.Add(new JournalLine
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            AccountCodeId = recvId,
            AccountCodeValue = recvCode,
            Debit = 0m,
            Credit = request.Amount,
            Description = desc,
        });

        _db.JournalEntries.Add(entry);
        correctionAccrual.JournalEntryId = entry.Id;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<CorrectionResult>.Success(new CorrectionResult(
            correctionAccrual.Id, correctionDetail.Id, entry.Id, request.Amount, requireApproval));
    }
}
