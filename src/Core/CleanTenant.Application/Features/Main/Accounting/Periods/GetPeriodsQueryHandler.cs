using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>
/// <see cref="GetPeriodsQuery"/> handler.
/// </summary>
public sealed class GetPeriodsQueryHandler
    : IRequestHandler<GetPeriodsQuery, Result<IReadOnlyList<PeriodListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetPeriodsQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PeriodListItem>>> Handle(
        GetPeriodsQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _db.AccountingPeriods
            .Where(p => p.CompanyId == query.CompanyId
                     && p.FiscalYearId == query.FiscalYearId
                     && !p.IsDeleted)
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .Select(p => new PeriodListItem(
                p.Id,
                p.FiscalYearId,
                p.Year,
                p.Month,
                p.StartDate,
                p.EndDate,
                p.Status))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PeriodListItem>>.Success(items.AsReadOnly());
    }
}
