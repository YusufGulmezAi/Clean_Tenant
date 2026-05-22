using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// <see cref="GetAccountCodesQuery"/> handler. Şirkete ait hesap planını listeler.
/// </summary>
public sealed class GetAccountCodesQueryHandler
    : IRequestHandler<GetAccountCodesQuery, Result<IReadOnlyList<AccountCodeListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetAccountCodesQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AccountCodeListItem>>> Handle(
        GetAccountCodesQuery query,
        CancellationToken cancellationToken)
    {
        var q = _db.AccountCodes
            .Where(ac => ac.CompanyId == query.CompanyId && !ac.IsDeleted);

        if (query.OnlyActive)
            q = q.Where(ac => ac.IsActive);

        if (query.OnlyDetail)
            q = q.Where(ac => ac.IsDetail);

        var items = await q
            .OrderBy(ac => ac.Code)
            .Select(ac => new AccountCodeListItem(
                ac.Id,
                ac.Code,
                ac.ParentCode,
                ac.Name,
                ac.Level,
                ac.AccountClass,
                ac.AccountType,
                ac.IsActive,
                ac.IsDetail,
                ac.IsMonetary,
                ac.IsRequired,
                ac.Source))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AccountCodeListItem>>.Success(items.AsReadOnly());
    }
}
