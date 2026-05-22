using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <see cref="CreateBudgetCommand"/> handler — Draft bütçe yaratır + boş bir
/// Draft <c>BudgetVersion</c> (V1) ekler (kalem versiyonları daha sonra eklenir).
/// </summary>
public sealed class CreateBudgetCommandHandler
    : IRequestHandler<CreateBudgetCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBudgetCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        // Mali yıl var mı + bu şirkete mi ait
        var fiscalYear = await _db.FiscalYears
            .FirstOrDefaultAsync(fy => fy.Id == request.FiscalYearId
                                    && fy.CompanyId == request.CompanyId
                                    && !fy.IsDeleted, cancellationToken);

        if (fiscalYear is null)
            return Result<Guid>.Failure(Error.NotFound("BDG-002", "Mali yıl bulunamadı."));

        // (CompanyId, FiscalYearId, Type, Title) benzersizliği
        var title = request.Title.Trim();
        var duplicate = await _db.Budgets
            .AnyAsync(b => b.CompanyId == request.CompanyId
                        && b.FiscalYearId == request.FiscalYearId
                        && b.Type == request.Type
                        && b.Title == title
                        && !b.IsDeleted, cancellationToken);

        if (duplicate)
            return Result<Guid>.Failure(
                Error.Conflict("BDG-001", "Bu mali yıl + tip için aynı isimde bütçe zaten mevcut."));

        // Bütçe dönemi — verilmediyse FiscalYear aralığından doldur
        var startYear = request.PeriodStartYear ?? fiscalYear.StartDate.Year;
        var startMonth = request.PeriodStartMonth ?? fiscalYear.StartDate.Month;
        var endYear = request.PeriodEndYear ?? fiscalYear.EndDate.Year;
        var endMonth = request.PeriodEndMonth ?? fiscalYear.EndDate.Month;

        // Dönem mali yıl aralığı içinde olmalı (ay granülaritesinde)
        var fyStartIdx = fiscalYear.StartDate.Year * 12 + fiscalYear.StartDate.Month;
        var fyEndIdx = fiscalYear.EndDate.Year * 12 + fiscalYear.EndDate.Month;
        var pStartIdx = startYear * 12 + startMonth;
        var pEndIdx = endYear * 12 + endMonth;

        if (pStartIdx < fyStartIdx || pEndIdx > fyEndIdx)
            return Result<Guid>.Failure(
                Error.Failure("BDG-003",
                    $"Bütçe dönemi mali yıl aralığı dışında ({fiscalYear.StartDate:MM.yyyy} - {fiscalYear.EndDate:MM.yyyy})."));

        if (pEndIdx < pStartIdx)
            return Result<Guid>.Failure(
                Error.Failure("BDG-004", "Bütçe bitiş dönemi başlangıçtan önce olamaz."));

        // Bütçe (Draft)
        var budget = new Budget
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            FiscalYearId = request.FiscalYearId,
            Type = request.Type,
            Title = title,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            PeriodStartYear = startYear,
            PeriodStartMonth = startMonth,
            PeriodEndYear = endYear,
            PeriodEndMonth = endMonth,
            Status = BudgetStatus.Draft
        };

        // Draft V1 — taslak versiyon; kalem versiyonları buna eklenir
        var draftVersion = new BudgetVersion
        {
            TenantId = request.TenantId,
            BudgetId = budget.Id,
            VersionNumber = 1,
            ValidFrom = null,
            ValidTo = null,
            PreviousVersionId = null,
            PublishedAt = null,
            PublishedBy = null,
            RevisionReason = null
        };
        budget.Versions.Add(draftVersion);

        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(budget.Id);
    }
}
