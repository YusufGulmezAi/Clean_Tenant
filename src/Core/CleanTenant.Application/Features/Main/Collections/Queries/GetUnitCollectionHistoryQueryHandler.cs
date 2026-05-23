using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Collections.Queries;

/// <summary><see cref="GetUnitCollectionHistoryQuery"/> handler.</summary>
public sealed class GetUnitCollectionHistoryQueryHandler
    : IRequestHandler<GetUnitCollectionHistoryQuery, Result<IReadOnlyList<CollectionListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUnitCollectionHistoryQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CollectionListItem>>> Handle(
        GetUnitCollectionHistoryQuery request, CancellationToken cancellationToken)
    {
        var q = _db.Collections
            .Where(c => c.CompanyId == request.CompanyId
                && c.UnitId == request.UnitId
                && !c.IsDeleted);

        if (request.From is { } from)
            q = q.Where(c => c.PaymentDate >= from);
        if (request.To is { } to)
            q = q.Where(c => c.PaymentDate <= to);

        var items = await q
            .OrderByDescending(c => c.PaymentDate).ThenByDescending(c => c.RecordedAt)
            .Select(c => new CollectionListItem(
                c.Id,
                c.UrlCode,
                c.UnitId,
                c.PaymentDate,
                c.Amount,
                c.Method,
                c.Reference,
                c.UnallocatedAmount,
                c.Allocations.Count(a => !a.IsDeleted),
                c.JournalEntryId,
                c.RecordedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CollectionListItem>>.Success(items.AsReadOnly());
    }
}
