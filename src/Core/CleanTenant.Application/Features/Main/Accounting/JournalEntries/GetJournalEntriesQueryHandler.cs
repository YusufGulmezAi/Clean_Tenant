using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="GetJournalEntriesQuery"/> handler. Sayfalı fiş listesi döner.
/// </summary>
public sealed class GetJournalEntriesQueryHandler
    : IRequestHandler<GetJournalEntriesQuery, Result<PagedResult<JournalEntryListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetJournalEntriesQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<PagedResult<JournalEntryListItem>>> Handle(
        GetJournalEntriesQuery query,
        CancellationToken cancellationToken)
    {
        var q = _db.JournalEntries
            .Where(je => je.CompanyId == query.CompanyId && !je.IsDeleted);

        if (query.AccountingPeriodId.HasValue)
            q = q.Where(je => je.AccountingPeriodId == query.AccountingPeriodId.Value);

        if (query.Status.HasValue)
            q = q.Where(je => je.Status == query.Status.Value);

        if (query.EntryType.HasValue)
            q = q.Where(je => je.EntryType == query.EntryType.Value);

        if (query.From.HasValue)
            q = q.Where(je => je.EntryDate >= query.From.Value);

        if (query.To.HasValue)
            q = q.Where(je => je.EntryDate <= query.To.Value);

        var totalCount = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(je => je.EntryDate)
            .ThenByDescending(je => je.EntryNumber)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(je => new JournalEntryListItem(
                je.Id,
                je.EntryNumber,
                je.EntryDate,
                je.EntryType,
                je.Description,
                je.TotalDebit,
                je.TotalCredit,
                je.Status))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<JournalEntryListItem>>.Success(
            new PagedResult<JournalEntryListItem>(items.AsReadOnly(), totalCount, query.Page, query.PageSize));
    }
}
