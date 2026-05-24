using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Parties;

/// <summary><see cref="UpdatePartyCommand"/> handler.</summary>
public sealed class UpdatePartyCommandHandler : IRequestHandler<UpdatePartyCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdatePartyCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdatePartyCommand request, CancellationToken cancellationToken)
    {
        var party = await _db.Parties.FirstOrDefaultAsync(
            p => p.Id == request.PartyId && p.CompanyId == request.CompanyId && !p.IsDeleted, cancellationToken);
        if (party is null)
            return Result.Failure(Error.NotFound("PTY-101", "Cari kişi bulunamadı."));

        var tckn = Normalize(request.Tckn);
        if (tckn is not null && tckn != party.Tckn)
        {
            var dup = await _db.Parties.AnyAsync(
                p => p.CompanyId == request.CompanyId && p.Tckn == tckn && p.Id != party.Id && !p.IsDeleted, cancellationToken);
            if (dup)
                return Result.Failure(Error.Conflict("PTY-201", $"'{tckn}' TCKN'li başka kişi mevcut."));
        }

        party.Kind = request.Kind;
        party.FullName = request.FullName.Trim();
        party.FirstName = Normalize(request.FirstName);
        party.LastName = Normalize(request.LastName);
        party.TradeName = Normalize(request.TradeName);
        party.Tckn = tckn;
        party.Vkn = Normalize(request.Vkn);
        party.BirthDate = request.BirthDate;
        party.Phone = Normalize(request.Phone);
        party.Email = Normalize(request.Email);
        party.AddressLine = Normalize(request.AddressLine);
        party.Notes = Normalize(request.Notes);
        party.TagsJson = Normalize(request.TagsJson);
        party.KvkkConsentGiven = request.KvkkConsentGiven;
        party.KvkkConsentAt = request.KvkkConsentAt;
        party.KvkkConsentChannel = Normalize(request.KvkkConsentChannel);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
