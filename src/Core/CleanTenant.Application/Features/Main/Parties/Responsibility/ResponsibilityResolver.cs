using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Parties.Enums;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Parties.Responsibility;

/// <summary>
/// <see cref="IResponsibilityResolver"/> implementasyonu — tenure'ları tek seferde
/// yükler, her BB için <see cref="ResponsibilityProrator"/> ile proration yapar.
/// </summary>
public sealed class ResponsibilityResolver : IResponsibilityResolver
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ResponsibilityResolver(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ResponsibilityResult>> ProrateBatchAsync(
        IReadOnlyCollection<UnitAccrualInput> units,
        int year, int month, ResponsibilityMode mode,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, ResponsibilityResult>(units.Count);
        if (units.Count == 0) return result;

        var unitIds = units.Select(u => u.UnitId).Distinct().ToList();
        var first = new DateOnly(year, month, 1);
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        // Aya dokunan (overlap eden) tenure'ları tek sorguda yükle
        var ownerships = (await _db.UnitOwnerships
            .Where(o => unitIds.Contains(o.UnitId) && !o.IsDeleted
                     && o.StartDate <= last && (o.EndDate == null || o.EndDate >= first))
            .Select(o => new { o.UnitId, o.PartyId, o.StartDate, o.EndDate, o.SharePercent })
            .ToListAsync(cancellationToken))
            .GroupBy(o => o.UnitId)
            .ToDictionary(g => g.Key, g => g
                .Select(o => new ResponsibilityProrator.OwnerWindow(o.PartyId, o.StartDate, o.EndDate, o.SharePercent))
                .ToList());

        var tenancies = (await _db.UnitTenancies
            .Where(t => unitIds.Contains(t.UnitId) && !t.IsDeleted
                     && t.StartDate <= last && (t.EndDate == null || t.EndDate >= first))
            .Select(t => new { t.UnitId, t.PartyId, t.StartDate, t.EndDate })
            .ToListAsync(cancellationToken))
            .GroupBy(t => t.UnitId)
            .ToDictionary(g => g.Key, g => g
                .Select(t => new ResponsibilityProrator.TenantWindow(t.PartyId, t.StartDate, t.EndDate))
                .ToList());

        var emptyOwners = new List<ResponsibilityProrator.OwnerWindow>();
        var emptyTenants = new List<ResponsibilityProrator.TenantWindow>();

        foreach (var u in units)
        {
            var owners = ownerships.GetValueOrDefault(u.UnitId, emptyOwners);
            var tenants = tenancies.GetValueOrDefault(u.UnitId, emptyTenants);
            result[u.UnitId] = ResponsibilityProrator.Prorate(year, month, u.Amount, mode, owners, tenants);
        }

        return result;
    }
}
