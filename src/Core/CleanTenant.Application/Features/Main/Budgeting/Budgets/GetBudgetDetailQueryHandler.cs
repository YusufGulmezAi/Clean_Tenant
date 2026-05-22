using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary><see cref="GetBudgetDetailQuery"/> handler.</summary>
public sealed class GetBudgetDetailQueryHandler
    : IRequestHandler<GetBudgetDetailQuery, Result<BudgetDetail>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBudgetDetailQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<BudgetDetail>> Handle(
        GetBudgetDetailQuery request,
        CancellationToken cancellationToken)
    {
        var budget = await (
            from b in _db.Budgets
            join fy in _db.FiscalYears on b.FiscalYearId equals fy.Id
            where b.Id == request.BudgetId
                && b.CompanyId == request.CompanyId
                && !b.IsDeleted && !fy.IsDeleted
            select new { Budget = b, FiscalYearLabel = fy.Label }
        ).FirstOrDefaultAsync(cancellationToken);

        if (budget is null)
            return Result<BudgetDetail>.Failure(Error.NotFound("BDG-100", "Bütçe bulunamadı."));

        // Versiyonlar + her versiyonun kalem sayısı/toplam tutarı
        var versions = await (
            from v in _db.BudgetVersions
            where v.BudgetId == request.BudgetId && !v.IsDeleted
            orderby v.VersionNumber
            select new BudgetVersionDto(
                v.Id,
                v.VersionNumber,
                v.ValidFrom,
                v.ValidTo,
                v.PublishedAt,
                v.PublishedBy,
                v.RevisionReason,
                _db.BudgetLineVersions.Count(lv => lv.BudgetVersionId == v.Id && !lv.IsDeleted),
                _db.BudgetLineVersions
                    .Where(lv => lv.BudgetVersionId == v.Id && !lv.IsDeleted)
                    .Sum(lv => (decimal?)lv.PlannedAmount) ?? 0m)
        ).ToListAsync(cancellationToken);

        var detail = new BudgetDetail(
            budget.Budget.Id,
            budget.Budget.CompanyId,
            budget.Budget.FiscalYearId,
            budget.FiscalYearLabel,
            budget.Budget.Title,
            budget.Budget.Notes,
            budget.Budget.Status,
            budget.Budget.CurrentVersionId,
            versions.AsReadOnly());

        return Result<BudgetDetail>.Success(detail);
    }
}
