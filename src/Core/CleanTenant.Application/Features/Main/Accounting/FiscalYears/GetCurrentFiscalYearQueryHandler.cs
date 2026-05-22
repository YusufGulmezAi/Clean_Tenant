using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>
/// <see cref="GetCurrentFiscalYearQuery"/> handler.
/// </summary>
public sealed class GetCurrentFiscalYearQueryHandler
    : IRequestHandler<GetCurrentFiscalYearQuery, Result<FiscalYearDetail>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetCurrentFiscalYearQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<FiscalYearDetail>> Handle(
        GetCurrentFiscalYearQuery query,
        CancellationToken cancellationToken)
    {
        var fy = await _db.FiscalYears
            .Where(x => x.CompanyId == query.CompanyId
                     && x.IsCurrentYear
                     && !x.IsDeleted)
            .Include(x => x.Periods.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        if (fy is null)
            return Result<FiscalYearDetail>.Failure(
                Error.NotFound("ACC-003", "Cari mali yıl bulunamadı."));

        var periods = fy.Periods
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .Select(p => new PeriodSummary(
                p.Id,
                p.Year,
                p.Month,
                p.StartDate,
                p.EndDate,
                p.Status))
            .ToList()
            .AsReadOnly();

        return Result<FiscalYearDetail>.Success(new FiscalYearDetail(
            fy.Id,
            fy.Label,
            fy.StartDate,
            fy.EndDate,
            fy.Status,
            fy.IsCurrentYear,
            periods));
    }
}
