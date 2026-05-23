using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Collections.Queries;

/// <summary><see cref="GetCollectionsByPeriodQuery"/> handler.</summary>
public sealed class GetCollectionsByPeriodQueryHandler
    : IRequestHandler<GetCollectionsByPeriodQuery, Result<IReadOnlyList<CollectionListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetCollectionsByPeriodQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CollectionListItem>>> Handle(
        GetCollectionsByPeriodQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.Collections
            .Where(c => c.CompanyId == request.CompanyId
                && c.AccountingPeriodId == request.AccountingPeriodId
                && !c.IsDeleted)
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
