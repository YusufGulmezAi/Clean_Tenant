using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.Domain.Identity.Tenants;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Caching.Readers;

/// <summary>
/// <para>
/// <see cref="ITenantCatalogReader"/>'ın hybrid cache + EF Core fallback
/// implementasyonu. Cache miss'te <see cref="ICatalogDbContext"/>'ten okur,
/// projection DTO'ya dönüştürür, cache'e yazar.
/// </para>
/// </summary>
public sealed class TenantCatalogReader : ITenantCatalogReader
{
    private readonly ICacheStore _cache;
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public TenantCatalogReader(ICacheStore cache, ICatalogDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TenantListItem>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Tenant.AllActive,
            async ct =>
            {
                var list = await _db.Tenants
                    .AsNoTracking()
                    .Where(t => t.Status == TenantStatus.Active)
                    .OrderBy(t => t.Name)
                    .Select(t => new TenantListItem(
                        t.Id,
                        t.UrlCode,
                        t.Name,
                        t.LegalName,
                        t.Status,
                        t.BillingTier,
                        t.AllowSystemWriteAccess))
                    .ToListAsync(ct);
                return (IReadOnlyList<TenantListItem>)list;
            },
            CacheOptions.ListShortLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<TenantListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<TenantListItem?>(
            CacheKeys.Tenant.ById(id),
            async ct => await _db.Tenants
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TenantListItem(
                    t.Id, t.UrlCode, t.Name, t.LegalName,
                    t.Status, t.BillingTier, t.AllowSystemWriteAccess))
                .FirstOrDefaultAsync(ct),
            CacheOptions.DetailMediumLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<TenantListItem?> GetByUrlCodeAsync(string urlCode, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<TenantListItem?>(
            CacheKeys.Tenant.ByUrlCode(urlCode),
            async ct => await _db.Tenants
                .AsNoTracking()
                .Where(t => t.UrlCode == urlCode)
                .Select(t => new TenantListItem(
                    t.Id, t.UrlCode, t.Name, t.LegalName,
                    t.Status, t.BillingTier, t.AllowSystemWriteAccess))
                .FirstOrDefaultAsync(ct),
            CacheOptions.DetailMediumLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<TenantDetail?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<TenantDetail?>(
            CacheKeys.Tenant.DetailById(id),
            async ct => await _db.Tenants
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TenantDetail(
                    t.Id,
                    t.UrlCode,
                    t.Name,
                    t.LegalName,
                    t.LegalIdentityType,
                    t.LegalIdentityNumber,
                    t.Address,
                    t.Status,
                    t.BillingTier,
                    t.HasDedicatedDatabase,
                    t.DatabaseSchemaName,
                    t.AllowSystemWriteAccess))
                .FirstOrDefaultAsync(ct),
            CacheOptions.DetailMediumLived,
            cancellationToken);
}
