using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>
/// <see cref="GetFiscalYearsQuery"/> handler.
/// </summary>
public sealed class GetFiscalYearsQueryHandler
    : IRequestHandler<GetFiscalYearsQuery, Result<IReadOnlyList<FiscalYearListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetFiscalYearsQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FiscalYearListItem>>> Handle(
        GetFiscalYearsQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _db.FiscalYears
            .Where(fy => fy.CompanyId == query.CompanyId && !fy.IsDeleted)
            .OrderByDescending(fy => fy.StartDate)
            .Select(fy => new FiscalYearListItem(
                fy.Id,
                fy.Label,
                fy.StartDate,
                fy.EndDate,
                fy.Status,
                fy.IsCurrentYear,
                fy.Periods.Count(p => !p.IsDeleted)))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<FiscalYearListItem>>.Success(items.AsReadOnly());
    }
}
