using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.Application.Features.Main.Readers;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Caching.Readers;

/// <summary>
/// <para>
/// <see cref="IMainCatalogReader"/>'ın hybrid cache + EF Core fallback
/// implementasyonu. Main DB'den projection DTO'lar üretir; Sistem-scope
/// "tüm siteler" sorgusunda parent Yönetim adlarını <see cref="ITenantCatalogReader"/>
/// üzerinden lookup eder (cascade cache).
/// </para>
/// </summary>
public sealed class MainCatalogReader : IMainCatalogReader
{
    private readonly ICacheStore _cache;
    private readonly IMainDbContext _db;
    private readonly ITenantCatalogReader _tenantReader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public MainCatalogReader(ICacheStore cache, IMainDbContext db, ITenantCatalogReader tenantReader)
    {
        _cache = cache;
        _db = db;
        _tenantReader = tenantReader;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CompanyListItem>> GetAllGlobalAsync(CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Company.AllGlobal,
            async ct =>
            {
                // Parent Yönetim adları için reader (kendi cache'ini kullanır)
                var tenants = await _tenantReader.GetAllActiveAsync(ct);
                var tenantNames = tenants.ToDictionary(t => t.Id, t => t.Name);

                var rows = await _db.Companies
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.TenantId, c.UrlCode, c.Name, c.LegalName, c.Vkn, c.Status })
                    .ToListAsync(ct);

                var list = rows.Select(c => new CompanyListItem(
                    c.Id,
                    c.TenantId,
                    tenantNames.TryGetValue(c.TenantId, out var tn) ? tn : null,
                    c.UrlCode,
                    c.Name,
                    c.LegalName,
                    c.Vkn,
                    c.Status)).ToList();

                return (IReadOnlyList<CompanyListItem>)list;
            },
            CacheOptions.ListShortLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<CompanyListItem>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Company.ByTenant(tenantId),
            async ct =>
            {
                var rows = await _db.Companies
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(c => c.TenantId == tenantId)
                    .OrderBy(c => c.Name)
                    .Select(c => new CompanyListItem(
                        c.Id, c.TenantId, null, c.UrlCode, c.Name, c.LegalName, c.Vkn, c.Status))
                    .ToListAsync(ct);
                return (IReadOnlyList<CompanyListItem>)rows;
            },
            CacheOptions.ListShortLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<CompanyListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<CompanyListItem?>(
            CacheKeys.Company.ById(id),
            async ct => await _db.Companies
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CompanyListItem(
                    c.Id, c.TenantId, null, c.UrlCode, c.Name, c.LegalName, c.Vkn, c.Status))
                .FirstOrDefaultAsync(ct),
            CacheOptions.DetailMediumLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<CompanyDetail?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<CompanyDetail?>(
            CacheKeys.Company.DetailById(id),
            async ct => await _db.Companies
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CompanyDetail(
                    c.Id, c.TenantId, c.UrlCode, c.Name, c.LegalName, c.Vkn, c.Email, c.Phone, c.Status))
                .FirstOrDefaultAsync(ct),
            CacheOptions.DetailMediumLived,
            cancellationToken);
}
