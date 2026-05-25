using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Collections.Queries;

/// <summary><see cref="GetCollectionContextQuery"/> handler.</summary>
public sealed class GetCollectionContextQueryHandler
    : IRequestHandler<GetCollectionContextQuery, Result<CollectionContext>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetCollectionContextQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<CollectionContext>> Handle(
        GetCollectionContextQuery request, CancellationToken cancellationToken)
    {
        var periods = await _db.AccountingPeriods
            .Where(p => p.CompanyId == request.CompanyId
                     && !p.IsDeleted
                     && p.Status == PeriodStatus.Open)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new OpenPeriodItem(p.Id, p.Year, p.Month))
            .ToListAsync(cancellationToken);

        // Aktif yaprak hesaplar — kasa/banka/çek (100/101/102) bellekte süzülür (~yüzlerce kayıt).
        var detailAccounts = await _db.AccountCodes
            .Where(a => a.CompanyId == request.CompanyId
                     && a.IsActive && a.IsDetail && !a.IsDeleted)
            .Select(a => new { a.Id, a.Code, a.Name })
            .ToListAsync(cancellationToken);

        var cashAccounts = detailAccounts
            .Select(a => new { a.Id, a.Code, a.Name, Kind = KindOf(a.Code) })
            .Where(a => a.Kind is not null)
            .OrderBy(a => a.Code, StringComparer.Ordinal)
            .Select(a => new CashAccountItem(a.Id, a.Code, a.Name, a.Kind!.Value))
            .ToList();

        return Result<CollectionContext>.Success(new CollectionContext(periods, cashAccounts));
    }

    /// <summary>Kod ön-ekinden kasa/çek/banka türünü çıkarır; değilse null.</summary>
    private static CashAccountKind? KindOf(string code)
    {
        if (code.StartsWith("100", StringComparison.Ordinal)) return CashAccountKind.Cash;
        if (code.StartsWith("101", StringComparison.Ordinal)) return CashAccountKind.Check;
        if (code.StartsWith("102", StringComparison.Ordinal)) return CashAccountKind.Bank;
        return null;
    }
}
