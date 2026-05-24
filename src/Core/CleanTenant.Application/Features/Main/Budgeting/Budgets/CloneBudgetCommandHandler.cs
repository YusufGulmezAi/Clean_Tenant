using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <see cref="CloneBudgetCommand"/> handler — kaynak bütçenin tasarımından yeni
/// Draft bütçe (V1) üretir (yenileme). Bkz. ReviseBudget kalem-kopyalama deseni.
/// </summary>
public sealed class CloneBudgetCommandHandler
    : IRequestHandler<CloneBudgetCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CloneBudgetCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CloneBudgetCommand request, CancellationToken cancellationToken)
    {
        // ── 1. Kaynak bütçe ───────────────────────────────────────────────────────
        var source = await _db.Budgets
            .FirstOrDefaultAsync(b => b.Id == request.SourceBudgetId
                                   && b.CompanyId == request.CompanyId
                                   && !b.IsDeleted, cancellationToken);
        if (source is null)
            return Result<Guid>.Failure(Error.NotFound("CLN-001", "Kaynak bütçe bulunamadı."));

        // ── 2. Yeni mali yıl ───────────────────────────────────────────────────────
        var fiscalYear = await _db.FiscalYears
            .FirstOrDefaultAsync(fy => fy.Id == request.NewFiscalYearId
                                    && fy.CompanyId == request.CompanyId
                                    && !fy.IsDeleted, cancellationToken);
        if (fiscalYear is null)
            return Result<Guid>.Failure(Error.NotFound("CLN-003", "Mali yıl bulunamadı."));

        // ── 3. (CompanyId, NewFiscalYearId, Type, NewTitle) benzersizliği ─────────
        var title = request.NewTitle.Trim();
        var duplicate = await _db.Budgets
            .AnyAsync(b => b.CompanyId == request.CompanyId
                        && b.FiscalYearId == request.NewFiscalYearId
                        && b.Type == source.Type
                        && b.Title == title
                        && !b.IsDeleted, cancellationToken);
        if (duplicate)
            return Result<Guid>.Failure(
                Error.Conflict("CLN-002", "Bu mali yıl + tip için aynı isimde bütçe zaten mevcut."));

        // ── 4. Dönem (verilmezse mali yıl aralığı) + sınır kontrolü ───────────────
        var startYear = request.PeriodStartYear ?? fiscalYear.StartDate.Year;
        var startMonth = request.PeriodStartMonth ?? fiscalYear.StartDate.Month;
        var endYear = request.PeriodEndYear ?? fiscalYear.EndDate.Year;
        var endMonth = request.PeriodEndMonth ?? fiscalYear.EndDate.Month;

        var fyStartIdx = fiscalYear.StartDate.Year * 12 + fiscalYear.StartDate.Month;
        var fyEndIdx = fiscalYear.EndDate.Year * 12 + fiscalYear.EndDate.Month;
        var pStartIdx = startYear * 12 + startMonth;
        var pEndIdx = endYear * 12 + endMonth;

        if (pStartIdx < fyStartIdx || pEndIdx > fyEndIdx)
            return Result<Guid>.Failure(Error.Failure("CLN-004",
                $"Bütçe dönemi mali yıl aralığı dışında ({fiscalYear.StartDate:MM.yyyy} - {fiscalYear.EndDate:MM.yyyy})."));
        if (pEndIdx < pStartIdx)
            return Result<Guid>.Failure(Error.Failure("CLN-005", "Bütçe bitiş dönemi başlangıçtan önce olamaz."));

        // ── 5. Kopyalanacak tasarım versiyonu (aktif yayınlı veya tek taslak) ─────
        Guid sourceVersionId;
        if (source.CurrentVersionId is { } current)
        {
            sourceVersionId = current;
        }
        else
        {
            var draft = await _db.BudgetVersions
                .Where(v => v.BudgetId == source.Id && !v.IsDeleted)
                .OrderBy(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);
            if (draft is null)
                return Result<Guid>.Failure(Error.Failure("CLN-006", "Kaynak bütçede versiyon bulunamadı."));
            sourceVersionId = draft.Id;
        }

        // ── 6. Kaynak kalem versiyonları + taksitler ──────────────────────────────
        var sourceLines = await _db.BudgetLineVersions
            .Where(lv => lv.BudgetVersionId == sourceVersionId && !lv.IsDeleted)
            .ToListAsync(cancellationToken);
        if (sourceLines.Count == 0)
            return Result<Guid>.Failure(Error.Failure("CLN-007", "Kaynak versiyonda kalem yok; kopyalama anlamsız."));

        var sourceLineIds = sourceLines.Select(lv => lv.Id).ToList();
        var installments = await _db.BudgetLineInstallments
            .Where(i => sourceLineIds.Contains(i.BudgetLineVersionId) && !i.IsDeleted)
            .ToListAsync(cancellationToken);
        var installmentsByLine = installments
            .GroupBy(i => i.BudgetLineVersionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Taksit aylarını yeni döneme öteleme miktarı (ay)
        var monthDelta = pStartIdx - (source.PeriodStartYear * 12 + source.PeriodStartMonth);

        // ── 7. Yeni Draft bütçe + V1 ──────────────────────────────────────────────
        var budget = new Budget
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            FiscalYearId = request.NewFiscalYearId,
            Type = source.Type,
            Title = title,
            Notes = source.Notes,
            PeriodStartYear = startYear,
            PeriodStartMonth = startMonth,
            PeriodEndYear = endYear,
            PeriodEndMonth = endMonth,
            Status = BudgetStatus.Draft,
        };
        var version = new BudgetVersion
        {
            TenantId = request.TenantId,
            BudgetId = budget.Id,
            VersionNumber = 1,
            ValidFrom = null,
            ValidTo = null,
            PreviousVersionId = null,
            PublishedAt = null,
        };
        budget.Versions.Add(version);
        _db.Budgets.Add(budget);

        // ── 8. Kalem versiyonları + taksitleri kopyala (taksit ayları ötelenir) ───
        var newLineVersions = new List<BudgetLineVersion>();
        var newInstallments = new List<BudgetLineInstallment>();

        foreach (var src in sourceLines)
        {
            var newLv = new BudgetLineVersion
            {
                TenantId = request.TenantId,
                BudgetVersionId = version.Id,
                BudgetLineId = src.BudgetLineId,
                PlannedAmount = src.PlannedAmount,
                PaymentSchedule = src.PaymentSchedule,
                DistributionModel = src.DistributionModel,
                ParticipationGroupId = src.ParticipationGroupId,
                DistributionConfig = src.DistributionConfig,
                DueDayOfMonth = src.DueDayOfMonth,
                InstallmentIntervalMonths = src.InstallmentIntervalMonths,
                IsManualOverride = false,
                OverrideReason = null,
            };

            if (src.InstallmentStartYear is { } isy && src.InstallmentStartMonth is { } ism)
            {
                var (y, m) = ShiftYearMonth(isy, ism, monthDelta);
                newLv.InstallmentStartYear = y;
                newLv.InstallmentStartMonth = m;
            }
            if (src.InstallmentEndYear is { } iey && src.InstallmentEndMonth is { } iem)
            {
                var (y, m) = ShiftYearMonth(iey, iem, monthDelta);
                newLv.InstallmentEndYear = y;
                newLv.InstallmentEndMonth = m;
            }

            newLineVersions.Add(newLv);

            foreach (var inst in installmentsByLine.GetValueOrDefault(src.Id, []))
            {
                var (iy, im) = ShiftYearMonth(inst.Year, inst.Month, monthDelta);
                newInstallments.Add(new BudgetLineInstallment
                {
                    TenantId = request.TenantId,
                    BudgetLineVersionId = newLv.Id,
                    InstallmentNumber = inst.InstallmentNumber,
                    Year = iy,
                    Month = im,
                    Amount = inst.Amount,
                    Label = inst.Label,
                    IsManuallyEdited = inst.IsManuallyEdited,
                });
            }
        }

        _db.BudgetLineVersions.AddRange(newLineVersions);
        _db.BudgetLineInstallments.AddRange(newInstallments);

        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(budget.Id);
    }

    /// <summary>(Yıl, Ay) çiftini ay-bazlı <paramref name="deltaMonths"/> kadar öteler.</summary>
    private static (int Year, int Month) ShiftYearMonth(int year, int month, int deltaMonths)
    {
        var zeroBased = (year * 12 + (month - 1)) + deltaMonths;
        return (zeroBased / 12, zeroBased % 12 + 1);
    }
}
