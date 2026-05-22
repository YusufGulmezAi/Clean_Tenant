using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Invoices;

/// <summary>
/// <see cref="GetInvoicesQuery"/> handler. Şirkete ait faturaları listeler.
/// </summary>
public sealed class GetInvoicesQueryHandler
    : IRequestHandler<GetInvoicesQuery, Result<IReadOnlyList<InvoiceListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetInvoicesQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InvoiceListItem>>> Handle(
        GetInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        var q = _db.Invoices
            .Where(inv => inv.CompanyId == query.CompanyId && !inv.IsDeleted);

        if (query.Direction.HasValue)
            q = q.Where(inv => inv.Direction == query.Direction.Value);

        if (query.OnlyUnposted)
            q = q.Where(inv => !inv.IsPostedToJournal);

        if (query.From.HasValue)
            q = q.Where(inv => inv.InvoiceDate >= query.From.Value);

        if (query.To.HasValue)
            q = q.Where(inv => inv.InvoiceDate <= query.To.Value);

        var items = await q
            .OrderByDescending(inv => inv.InvoiceDate)
            .Select(inv => new InvoiceListItem(
                inv.Id,
                inv.InvoiceNumber,
                inv.InvoiceDate,
                inv.Direction,
                inv.CounterpartyName,
                inv.TotalAmount,
                inv.IsPostedToJournal))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<InvoiceListItem>>.Success(items.AsReadOnly());
    }
}
