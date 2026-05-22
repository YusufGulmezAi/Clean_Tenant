using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="GetJournalEntryDetailQuery"/> handler.
/// Yevmiye fişini satırlarıyla birlikte getirir.
/// </summary>
public sealed class GetJournalEntryDetailQueryHandler
    : IRequestHandler<GetJournalEntryDetailQuery, Result<JournalEntryDetail>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetJournalEntryDetailQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<JournalEntryDetail>> Handle(
        GetJournalEntryDetailQuery query,
        CancellationToken cancellationToken)
    {
        var entry = await _db.JournalEntries
            .Include(je => je.Lines)
            .FirstOrDefaultAsync(
                je => je.Id == query.EntryId
                   && je.CompanyId == query.CompanyId
                   && !je.IsDeleted,
                cancellationToken);

        if (entry is null)
            return Result<JournalEntryDetail>.Failure(
                Error.NotFound("ACC-004", "Yevmiye fişi bulunamadı."));

        var lines = entry.Lines
            .Select(l => new JournalLineDetail(
                l.Id,
                l.AccountCodeId,
                l.AccountCodeValue,
                l.Debit,
                l.Credit,
                l.Description,
                l.CostCenterId,
                l.TaxCode))
            .ToList()
            .AsReadOnly();

        return Result<JournalEntryDetail>.Success(new JournalEntryDetail(
            entry.Id,
            entry.EntryNumber,
            entry.EntryDate,
            entry.EntryType,
            entry.Description,
            entry.Reference,
            entry.ReferenceId,
            entry.TotalDebit,
            entry.TotalCredit,
            entry.Status,
            entry.PostedAt,
            entry.PostedBy,
            entry.ApprovedAt,
            entry.ApprovedBy,
            entry.VoidReason,
            entry.OriginalEntryId,
            lines));
    }
}
