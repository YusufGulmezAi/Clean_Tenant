using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Common.Security;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Parties;

/// <summary><see cref="GetPartyDetailQuery"/> handler (PII izin-bazlı maskeleme).</summary>
public sealed class GetPartyDetailQueryHandler : IRequestHandler<GetPartyDetailQuery, Result<PartyDetail>>
{
    private readonly IMainDbContext _db;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetPartyDetailQueryHandler(IMainDbContext db, ICurrentSessionAccessor session)
    {
        _db = db;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result<PartyDetail>> Handle(GetPartyDetailQuery request, CancellationToken cancellationToken)
    {
        var p = await _db.Parties
            .Where(x => x.Id == request.PartyId && x.CompanyId == request.CompanyId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
        if (p is null)
            return Result<PartyDetail>.Failure(Error.NotFound("PTY-101", "Cari kişi bulunamadı."));

        var unmask = CanViewPii(_session.Current);
        var dto = new PartyDetail(
            p.Id, p.UrlCode, p.Kind, p.FullName, p.FirstName, p.LastName, p.TradeName,
            unmask ? p.Tckn : PiiMasker.MaskTckn(p.Tckn),
            unmask ? p.Vkn : PiiMasker.MaskVkn(p.Vkn),
            p.BirthDate,
            unmask ? p.Phone : PiiMasker.MaskPhone(p.Phone),
            p.Email, p.AddressLine, p.Notes, p.TagsJson,
            p.KvkkConsentGiven, p.KvkkConsentAt, p.KvkkConsentChannel,
            PiiMasked: !unmask);

        return Result<PartyDetail>.Success(dto);
    }

    private static bool CanViewPii(AuthSession? s)
        => s is not null && (s.ScopeLevel == ScopeLevel.System || s.Permissions.Contains("tenant.party.pii.view"));
}
