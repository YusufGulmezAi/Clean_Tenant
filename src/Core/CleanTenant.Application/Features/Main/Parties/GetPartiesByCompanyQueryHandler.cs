using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Common.Security;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Parties;

/// <summary><see cref="GetPartiesByCompanyQuery"/> handler (arama + PII maskeleme).</summary>
public sealed class GetPartiesByCompanyQueryHandler
    : IRequestHandler<GetPartiesByCompanyQuery, Result<IReadOnlyList<PartyListItem>>>
{
    private readonly IMainDbContext _db;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetPartiesByCompanyQueryHandler(IMainDbContext db, ICurrentSessionAccessor session)
    {
        _db = db;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PartyListItem>>> Handle(
        GetPartiesByCompanyQuery request, CancellationToken cancellationToken)
    {
        var q = _db.Parties.Where(p => p.CompanyId == request.CompanyId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(p => p.FullName.Contains(s, StringComparison.OrdinalIgnoreCase)
                          || (p.Tckn != null && p.Tckn.Contains(s, StringComparison.OrdinalIgnoreCase))
                          || (p.Phone != null && p.Phone.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        var take = request.Take is > 0 and <= 200 ? request.Take : 50;
        var rows = await q.OrderBy(p => p.FullName)
            .Take(take)
            .Select(p => new { p.Id, p.UrlCode, p.Kind, p.FullName, p.Tckn, p.Phone, p.Email })
            .ToListAsync(cancellationToken);

        var unmask = CanViewPii(_session.Current);
        var items = rows.Select(p => new PartyListItem(
            p.Id, p.UrlCode, p.Kind, p.FullName,
            unmask ? p.Tckn : PiiMasker.MaskTckn(p.Tckn),
            unmask ? p.Phone : PiiMasker.MaskPhone(p.Phone),
            p.Email)).ToList();

        return Result<IReadOnlyList<PartyListItem>>.Success(items.AsReadOnly());
    }

    private static bool CanViewPii(AuthSession? s)
        => s is not null && (s.ScopeLevel == ScopeLevel.System || s.Permissions.Contains("tenant.party.pii.view"));
}
