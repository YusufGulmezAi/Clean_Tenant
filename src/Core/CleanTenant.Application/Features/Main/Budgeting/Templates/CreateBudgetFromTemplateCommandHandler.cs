using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary>
/// <see cref="CreateBudgetFromTemplateCommand"/> handler — Catalog şablonunu hedef
/// şirkette kod-eşlemeli instantiate eder (yapı-only → tutarlar 0).
/// </summary>
public sealed class CreateBudgetFromTemplateCommandHandler
    : IRequestHandler<CreateBudgetFromTemplateCommand, Result<Guid>>
{
    private readonly IMainDbContext _main;
    private readonly ICatalogDbContext _catalog;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBudgetFromTemplateCommandHandler(IMainDbContext main, ICatalogDbContext catalog)
    {
        _main = main;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateBudgetFromTemplateCommand request, CancellationToken cancellationToken)
    {
        // ── 1. Şablon (görünürlük erişim kontrolü) ───────────────────────────────
        var template = await _catalog.BudgetTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && !t.IsDeleted
                && (t.Visibility == TemplateVisibility.Public
                    || t.OwnerTenantId == null
                    || t.OwnerTenantId == request.TenantId), cancellationToken);
        if (template is null)
            return Result<Guid>.Failure(Error.NotFound("CFT-001", "Bütçe şablonu bulunamadı veya erişilemez."));

        var tLines = await _catalog.BudgetTemplateLines
            .Where(l => l.BudgetTemplateId == template.Id && !l.IsDeleted)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync(cancellationToken);
        if (tLines.Count == 0)
            return Result<Guid>.Failure(Error.Failure("CFT-006", "Şablonda kalem yok."));

        // ── 2. Mali yıl ───────────────────────────────────────────────────────────
        var fiscalYear = await _main.FiscalYears
            .FirstOrDefaultAsync(f => f.Id == request.FiscalYearId
                                   && f.CompanyId == request.CompanyId
                                   && !f.IsDeleted, cancellationToken);
        if (fiscalYear is null)
            return Result<Guid>.Failure(Error.NotFound("CFT-002", "Mali yıl bulunamadı."));

        // ── 3. Benzersizlik ───────────────────────────────────────────────────────
        var title = request.Title.Trim();
        var duplicate = await _main.Budgets.AnyAsync(b => b.CompanyId == request.CompanyId
            && b.FiscalYearId == request.FiscalYearId && b.Type == template.Type
            && b.Title == title && !b.IsDeleted, cancellationToken);
        if (duplicate)
            return Result<Guid>.Failure(Error.Conflict("CFT-003", "Bu mali yıl + tip için aynı isimde bütçe zaten mevcut."));

        // ── 4. Dönem ────────────────────────────────────────────────────────────────
        var startYear = request.PeriodStartYear ?? fiscalYear.StartDate.Year;
        var startMonth = request.PeriodStartMonth ?? fiscalYear.StartDate.Month;
        var endYear = request.PeriodEndYear ?? fiscalYear.EndDate.Year;
        var endMonth = request.PeriodEndMonth ?? fiscalYear.EndDate.Month;

        var fyStartIdx = fiscalYear.StartDate.Year * 12 + fiscalYear.StartDate.Month;
        var fyEndIdx = fiscalYear.EndDate.Year * 12 + fiscalYear.EndDate.Month;
        var pStartIdx = startYear * 12 + startMonth;
        var pEndIdx = endYear * 12 + endMonth;
        if (pStartIdx < fyStartIdx || pEndIdx > fyEndIdx)
            return Result<Guid>.Failure(Error.Failure("CFT-004",
                $"Bütçe dönemi mali yıl aralığı dışında ({fiscalYear.StartDate:MM.yyyy} - {fiscalYear.EndDate:MM.yyyy})."));
        if (pEndIdx < pStartIdx)
            return Result<Guid>.Failure(Error.Failure("CFT-005", "Bütçe bitiş dönemi başlangıçtan önce olamaz."));

        // ── 5. Hedef şirkette mevcut kod sözlükleri (find-or-create için) ─────────
        var catByCode = (await _main.ExpenseCategories
            .Where(c => c.CompanyId == request.CompanyId && !c.IsDeleted)
            .ToListAsync(cancellationToken))
            .ToDictionary(c => c.Code, c => c);
        var lineByCode = (await _main.BudgetLines
            .Where(l => l.CompanyId == request.CompanyId && !l.IsDeleted)
            .ToListAsync(cancellationToken))
            .ToDictionary(l => l.Code, l => l);
        var groupByCode = (await _main.ParticipationGroups
            .Where(g => g.CompanyId == request.CompanyId && !g.IsDeleted)
            .ToListAsync(cancellationToken))
            .ToDictionary(g => g.Code, g => g);

        ExpenseCategory GetOrCreateCategory(string code, string name, string? parentCode)
        {
            if (catByCode.TryGetValue(code, out var existing))
                return existing;
            Guid? parentId = null;
            if (!string.IsNullOrWhiteSpace(parentCode))
                parentId = GetOrCreateCategory(parentCode!, parentCode!, null).Id; // parent adı bilinmiyor → kod
            var cat = new ExpenseCategory
            {
                TenantId = request.TenantId, CompanyId = request.CompanyId,
                Code = code, Name = name, ParentCategoryId = parentId, DisplayOrder = 0,
            };
            _main.ExpenseCategories.Add(cat);
            catByCode[code] = cat;
            return cat;
        }

        // ── 6. Yeni Draft bütçe + V1 ──────────────────────────────────────────────
        var budget = new Budget
        {
            TenantId = request.TenantId, CompanyId = request.CompanyId,
            FiscalYearId = request.FiscalYearId, Type = template.Type, Title = title,
            PeriodStartYear = startYear, PeriodStartMonth = startMonth,
            PeriodEndYear = endYear, PeriodEndMonth = endMonth,
            Status = BudgetStatus.Draft,
        };
        var version = new BudgetVersion
        {
            TenantId = request.TenantId, BudgetId = budget.Id, VersionNumber = 1,
            ValidFrom = null, ValidTo = null, PreviousVersionId = null, PublishedAt = null,
        };
        budget.Versions.Add(version);
        _main.Budgets.Add(budget);

        var newLineVersions = new List<BudgetLineVersion>();
        var newInstallments = new List<BudgetLineInstallment>();

        foreach (var tl in tLines)
        {
            var category = GetOrCreateCategory(tl.CategoryCode, tl.CategoryName, tl.ParentCategoryCode);

            if (!lineByCode.TryGetValue(tl.LineCode, out var line))
            {
                line = new BudgetLine
                {
                    TenantId = request.TenantId, CompanyId = request.CompanyId,
                    ExpenseCategoryId = category.Id, Code = tl.LineCode, Name = tl.LineName,
                    Description = tl.LineDescription, IsActive = true, DisplayOrder = tl.DisplayOrder,
                };
                _main.BudgetLines.Add(line);
                lineByCode[tl.LineCode] = line;
            }

            Guid? groupId = null;
            if (!string.IsNullOrWhiteSpace(tl.ParticipationGroupCode))
            {
                if (!groupByCode.TryGetValue(tl.ParticipationGroupCode!, out var group))
                {
                    group = new ParticipationGroup
                    {
                        TenantId = request.TenantId, CompanyId = request.CompanyId,
                        Code = tl.ParticipationGroupCode!, Name = tl.ParticipationGroupName ?? tl.ParticipationGroupCode!,
                        IsActive = true,
                    };
                    _main.ParticipationGroups.Add(group);
                    groupByCode[tl.ParticipationGroupCode!] = group;
                }
                groupId = group.Id;
            }

            var lv = new BudgetLineVersion
            {
                TenantId = request.TenantId, BudgetVersionId = version.Id, BudgetLineId = line.Id,
                PlannedAmount = 0m, // yapı-only: site doldurur
                PaymentSchedule = tl.PaymentSchedule, DistributionModel = tl.DistributionModel,
                ParticipationGroupId = groupId, DistributionConfig = tl.DistributionConfig,
                DueDayOfMonth = tl.DueDayOfMonth, InstallmentIntervalMonths = tl.InstallmentIntervalMonths,
                IsManualOverride = false,
            };

            // Installment kalemler için sıfır-tutarlı taksit ızgarası (yeni dönem başından)
            if (tl.PaymentSchedule == PaymentSchedule.Installment
                && tl.InstallmentCount is { } count && count > 0
                && tl.InstallmentIntervalMonths is { } interval && interval > 0)
            {
                lv.InstallmentStartYear = startYear;
                lv.InstallmentStartMonth = startMonth;
                for (var i = 0; i < count; i++)
                {
                    var (yy, mm) = ShiftYearMonth(startYear, startMonth, i * interval);
                    newInstallments.Add(new BudgetLineInstallment
                    {
                        TenantId = request.TenantId, BudgetLineVersionId = lv.Id,
                        InstallmentNumber = i + 1, Year = yy, Month = mm, Amount = 0m,
                    });
                    if (i == count - 1) { lv.InstallmentEndYear = yy; lv.InstallmentEndMonth = mm; }
                }
            }

            newLineVersions.Add(lv);
        }

        _main.BudgetLineVersions.AddRange(newLineVersions);
        _main.BudgetLineInstallments.AddRange(newInstallments);

        await _main.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(budget.Id);
    }

    /// <summary>(Yıl, Ay) çiftini ay-bazlı öteler.</summary>
    private static (int Year, int Month) ShiftYearMonth(int year, int month, int deltaMonths)
    {
        var zeroBased = (year * 12 + (month - 1)) + deltaMonths;
        return (zeroBased / 12, zeroBased % 12 + 1);
    }
}
