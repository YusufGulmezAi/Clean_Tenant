using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accruals.Queries;

/// <summary><see cref="GetAccrualsByPeriodQuery"/> handler.</summary>
public sealed class GetAccrualsByPeriodQueryHandler
    : IRequestHandler<GetAccrualsByPeriodQuery, Result<IReadOnlyList<AccrualListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetAccrualsByPeriodQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AccrualListItem>>> Handle(
        GetAccrualsByPeriodQuery request, CancellationToken cancellationToken)
    {
        var q = _db.Accruals
            .Where(a => a.CompanyId == request.CompanyId && a.Year == request.Year && !a.IsDeleted);

        if (request.Month is { } m)
            q = q.Where(a => a.Month == m);

        var items = await q
            .OrderByDescending(a => a.Year).ThenByDescending(a => a.Month).ThenBy(a => a.Source)
            .Select(a => new AccrualListItem(
                a.Id,
                a.Source,
                a.Year,
                a.Month,
                a.Description,
                a.TotalAmount,
                a.Details.Count(d => !d.IsDeleted),
                a.JournalEntryId,
                a.GeneratedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AccrualListItem>>.Success(items.AsReadOnly());
    }
}
