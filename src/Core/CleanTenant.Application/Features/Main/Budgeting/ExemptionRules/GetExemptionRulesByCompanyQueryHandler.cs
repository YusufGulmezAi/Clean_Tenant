using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.ExemptionRules;

/// <summary><see cref="GetExemptionRulesByCompanyQuery"/> handler.</summary>
public sealed class GetExemptionRulesByCompanyQueryHandler
    : IRequestHandler<GetExemptionRulesByCompanyQuery, Result<IReadOnlyList<ExemptionRuleListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetExemptionRulesByCompanyQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExemptionRuleListItem>>> Handle(
        GetExemptionRulesByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        var q = from e in _db.ExemptionRules
                join u in _db.Units on e.UnitId equals u.Id
                join bl in _db.BudgetLines on e.BudgetLineId equals bl.Id
                where e.CompanyId == request.CompanyId
                    && !e.IsDeleted && !u.IsDeleted && !bl.IsDeleted
                select new { Exemption = e, Unit = u, Line = bl };

        if (request.UnitId is { } unitId)
            q = q.Where(x => x.Unit.Id == unitId);
        if (request.BudgetLineId is { } lineId)
            q = q.Where(x => x.Line.Id == lineId);

        var items = await q
            .OrderByDescending(x => x.Exemption.ValidFrom)
            .Select(x => new ExemptionRuleListItem(
                x.Exemption.Id,
                x.Unit.Id,
                x.Unit.Number,
                x.Line.Id,
                x.Line.Code,
                x.Line.Name,
                x.Exemption.ValidFrom,
                x.Exemption.ValidTo,
                x.Exemption.Reason))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ExemptionRuleListItem>>.Success(items.AsReadOnly());
    }
}
