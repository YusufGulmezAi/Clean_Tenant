using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary><see cref="UpdateBudgetCommand"/> handler.</summary>
public sealed class UpdateBudgetCommandHandler : IRequestHandler<UpdateBudgetCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateBudgetCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await _db.Budgets
            .FirstOrDefaultAsync(b => b.Id == request.BudgetId
                                   && b.CompanyId == request.CompanyId
                                   && !b.IsDeleted, cancellationToken);
        if (budget is null)
            return Result.Failure(Error.NotFound("UBG-001", "Bütçe bulunamadı."));
        if (budget.Status != BudgetStatus.Draft)
            return Result.Failure(Error.Failure("UBG-002", "Yalnız taslak bütçe düzenlenebilir; yayınlı bütçeyi revize edin."));

        var title = request.Title.Trim();
        if (!string.Equals(title, budget.Title, StringComparison.Ordinal))
        {
            var duplicate = await _db.Budgets.AnyAsync(b => b.CompanyId == request.CompanyId
                && b.FiscalYearId == budget.FiscalYearId && b.Type == budget.Type
                && b.Title == title && b.Id != budget.Id && !b.IsDeleted, cancellationToken);
            if (duplicate)
                return Result.Failure(Error.Conflict("UBG-003", "Bu mali yıl + tip için aynı isimde bütçe zaten mevcut."));
        }

        if (request.PeriodStartYear is not null)
        {
            var fiscalYear = await _db.FiscalYears
                .FirstOrDefaultAsync(f => f.Id == budget.FiscalYearId && !f.IsDeleted, cancellationToken);
            if (fiscalYear is null)
                return Result.Failure(Error.NotFound("UBG-004", "Mali yıl bulunamadı."));

            var startYear = request.PeriodStartYear.Value;
            var startMonth = request.PeriodStartMonth ?? fiscalYear.StartDate.Month;
            var endYear = request.PeriodEndYear ?? fiscalYear.EndDate.Year;
            var endMonth = request.PeriodEndMonth ?? fiscalYear.EndDate.Month;

            var fyStartIdx = fiscalYear.StartDate.Year * 12 + fiscalYear.StartDate.Month;
            var fyEndIdx = fiscalYear.EndDate.Year * 12 + fiscalYear.EndDate.Month;
            var pStartIdx = startYear * 12 + startMonth;
            var pEndIdx = endYear * 12 + endMonth;
            if (pStartIdx < fyStartIdx || pEndIdx > fyEndIdx)
                return Result.Failure(Error.Failure("UBG-005", "Bütçe dönemi mali yıl aralığı dışında."));
            if (pEndIdx < pStartIdx)
                return Result.Failure(Error.Failure("UBG-006", "Bütçe bitiş dönemi başlangıçtan önce olamaz."));

            budget.PeriodStartYear = startYear;
            budget.PeriodStartMonth = startMonth;
            budget.PeriodEndYear = endYear;
            budget.PeriodEndMonth = endMonth;
        }

        budget.Title = title;
        budget.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
