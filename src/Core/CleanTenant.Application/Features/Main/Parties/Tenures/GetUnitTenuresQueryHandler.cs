using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary><see cref="GetUnitTenuresQuery"/> handler — malik/kiracı/iletişim ağacı.</summary>
public sealed class GetUnitTenuresQueryHandler : IRequestHandler<GetUnitTenuresQuery, Result<UnitTenures>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUnitTenuresQueryHandler(IMainDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<UnitTenures>> Handle(GetUnitTenuresQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        var unitInfo = await (
            from u in _db.Units
            join b in _db.Buildings on u.BuildingId equals b.Id
            where u.Id == request.UnitId && !u.IsDeleted && !b.IsDeleted
            select new { u.Number, BuildingName = b.Name }
        ).FirstOrDefaultAsync(cancellationToken);
        if (unitInfo is null)
            return Result<UnitTenures>.Failure(Error.NotFound("TEN-201", "Bağımsız bölüm bulunamadı."));

        var owners = await (
            from o in _db.UnitOwnerships
            join p in _db.Parties on o.PartyId equals p.Id
            where o.UnitId == request.UnitId && !o.IsDeleted && !p.IsDeleted
            select new OwnershipItem(
                o.Id, o.PartyId, p.FullName, p.UrlCode, o.SharePercent, o.IsJointAndSeveral,
                o.StartDate, o.EndDate,
                o.StartDate <= today && (o.EndDate == null || o.EndDate >= today),
                o.Notes)
        ).ToListAsync(cancellationToken);

        var tenants = await (
            from t in _db.UnitTenancies
            join p in _db.Parties on t.PartyId equals p.Id
            where t.UnitId == request.UnitId && !t.IsDeleted && !p.IsDeleted
            select new TenancyItem(
                t.Id, t.PartyId, p.FullName, p.UrlCode, t.StartDate, t.EndDate,
                t.StartDate <= today && (t.EndDate == null || t.EndDate >= today),
                t.Notes)
        ).ToListAsync(cancellationToken);

        var contacts = await (
            from c in _db.UnitContacts
            join p in _db.Parties on c.PartyId equals p.Id
            where c.UnitId == request.UnitId && !c.IsDeleted && !p.IsDeleted
            select new ContactItem(
                c.Id, c.PartyId, p.FullName, p.UrlCode, c.ContactRole, c.StartDate, c.EndDate,
                c.StartDate <= today && (c.EndDate == null || c.EndDate >= today),
                c.Notes)
        ).ToListAsync(cancellationToken);

        // Malikler: aktif en üstte, sonra başlangıç tarihine göre yeni→eski
        var orderedOwners = owners
            .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.StartDate).ThenByDescending(x => x.SharePercent)
            .ToList();
        // Kiracılar: son kiracı en üstte
        var orderedTenants = tenants
            .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.StartDate)
            .ToList();
        var orderedContacts = contacts
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.ContactRole)
            .ToList();

        return Result<UnitTenures>.Success(new UnitTenures(
            unitInfo.Number, unitInfo.BuildingName,
            orderedOwners.AsReadOnly(), orderedTenants.AsReadOnly(), orderedContacts.AsReadOnly()));
    }
}
