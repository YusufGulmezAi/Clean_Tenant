using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Parties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Parties;

/// <summary><see cref="CreatePartyCommand"/> handler. TCKN benzersizliği (PTY-201).</summary>
public sealed class CreatePartyCommandHandler : IRequestHandler<CreatePartyCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreatePartyCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreatePartyCommand request, CancellationToken cancellationToken)
    {
        var tckn = Normalize(request.Tckn);
        if (tckn is not null)
        {
            var dup = await _db.Parties.AnyAsync(
                p => p.CompanyId == request.CompanyId && p.Tckn == tckn && !p.IsDeleted, cancellationToken);
            if (dup)
                return Result<Guid>.Failure(Error.Conflict("PTY-201", $"'{tckn}' TCKN'li kişi zaten kayıtlı."));
        }

        var party = new Party
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            Kind = request.Kind,
            FullName = request.FullName.Trim(),
            FirstName = Normalize(request.FirstName),
            LastName = Normalize(request.LastName),
            TradeName = Normalize(request.TradeName),
            Tckn = tckn,
            Vkn = Normalize(request.Vkn),
            BirthDate = request.BirthDate,
            Phone = Normalize(request.Phone),
            Email = Normalize(request.Email),
            AddressLine = Normalize(request.AddressLine),
            Notes = Normalize(request.Notes),
            TagsJson = Normalize(request.TagsJson),
            KvkkConsentGiven = request.KvkkConsentGiven,
            KvkkConsentAt = request.KvkkConsentAt,
            KvkkConsentChannel = Normalize(request.KvkkConsentChannel),
        };

        _db.Parties.Add(party);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(party.Id);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
