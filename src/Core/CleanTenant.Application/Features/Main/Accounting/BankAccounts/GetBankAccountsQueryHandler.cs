using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>
/// <see cref="GetBankAccountsQuery"/> handler. Şirkete ait banka hesaplarını listeler.
/// </summary>
public sealed class GetBankAccountsQueryHandler
    : IRequestHandler<GetBankAccountsQuery, Result<IReadOnlyList<BankAccountListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBankAccountsQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BankAccountListItem>>> Handle(
        GetBankAccountsQuery query,
        CancellationToken cancellationToken)
    {
        var q = _db.AccountingBankAccounts
            .Where(ba => ba.CompanyId == query.CompanyId && !ba.IsDeleted);

        if (query.OnlyActive)
            q = q.Where(ba => ba.IsActive);

        var items = await q
            .OrderBy(ba => ba.Name)
            .Select(ba => new BankAccountListItem(
                ba.Id,
                ba.Name,
                ba.BankName,
                ba.Iban,
                ba.AccountType,
                ba.CurrencyCode,
                ba.IsActive))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BankAccountListItem>>.Success(items.AsReadOnly());
    }
}
