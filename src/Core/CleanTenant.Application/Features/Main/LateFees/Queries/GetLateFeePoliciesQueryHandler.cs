using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.LateFees.Queries;

/// <summary><see cref="GetLateFeePoliciesQuery"/> handler.</summary>
public sealed class GetLateFeePoliciesQueryHandler
    : IRequestHandler<GetLateFeePoliciesQuery, Result<IReadOnlyList<LateFeePolicyItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetLateFeePoliciesQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LateFeePolicyItem>>> Handle(
        GetLateFeePoliciesQuery request, CancellationToken cancellationToken)
    {
        var items = await (
            from p in _db.LateFeePolicies
            where p.CompanyId == request.CompanyId && !p.IsDeleted
            join b in _db.Budgets on p.BudgetId equals b.Id into bj
            from b in bj.DefaultIfEmpty()
            join ac in _db.AccountCodes on p.IncomeAccountCodeId equals ac.Id into acj
            from ac in acj.DefaultIfEmpty()
            orderby p.BudgetId == null descending
            select new LateFeePolicyItem(
                p.Id,
                p.BudgetId,
                b != null ? b.Title : null,
                p.MonthlyRatePercent,
                p.IsCompound,
                p.GraceDays,
                p.IncomeAccountCodeId,
                ac != null ? ac.Code : null,
                p.IsActive)
        ).ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LateFeePolicyItem>>.Success(items.AsReadOnly());
    }
}
