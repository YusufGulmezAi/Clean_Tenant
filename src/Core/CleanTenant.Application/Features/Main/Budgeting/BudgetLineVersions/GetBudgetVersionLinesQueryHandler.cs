using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary><see cref="GetBudgetVersionLinesQuery"/> handler.</summary>
public sealed class GetBudgetVersionLinesQueryHandler
    : IRequestHandler<GetBudgetVersionLinesQuery, Result<IReadOnlyList<BudgetLineVersionDto>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBudgetVersionLinesQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BudgetLineVersionDto>>> Handle(
        GetBudgetVersionLinesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await (
            from lv in _db.BudgetLineVersions
            join v in _db.BudgetVersions on lv.BudgetVersionId equals v.Id
            join b in _db.Budgets on v.BudgetId equals b.Id
            join line in _db.BudgetLines on lv.BudgetLineId equals line.Id
            join cat in _db.ExpenseCategories on line.ExpenseCategoryId equals cat.Id
            join pg in _db.ParticipationGroups on lv.ParticipationGroupId equals (Guid?)pg.Id into pgJoin
            from pg in pgJoin.DefaultIfEmpty()
            where lv.BudgetVersionId == request.BudgetVersionId
                && b.CompanyId == request.CompanyId
                && !lv.IsDeleted && !v.IsDeleted && !b.IsDeleted
                && !line.IsDeleted && !cat.IsDeleted
            orderby cat.DisplayOrder, line.DisplayOrder, line.Code
            select new BudgetLineVersionDto(
                lv.Id,
                lv.BudgetLineId,
                line.Code,
                line.Name,
                line.ExpenseCategoryId,
                cat.Code,
                cat.Name,
                lv.PlannedAmount,
                lv.PaymentSchedule,
                lv.DistributionModel,
                lv.ParticipationGroupId,
                pg != null ? pg.Name : null,
                lv.IsManualOverride,
                lv.OverrideReason,
                lv.DueDayOfMonth)
        ).ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BudgetLineVersionDto>>.Success(items.AsReadOnly());
    }
}
