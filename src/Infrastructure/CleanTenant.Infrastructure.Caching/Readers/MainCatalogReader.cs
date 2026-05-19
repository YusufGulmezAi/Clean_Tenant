using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.Application.Features.Main.Readers;
using CleanTenant.Infrastructure.Persistence.Main;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Caching.Readers;

/// <summary>
/// <para>
/// <see cref="IMainCatalogReader"/>'ın hybrid cache + EF Core fallback
/// implementasyonu. Main DB'den projection DTO'lar üretir; Sistem-scope
/// "tüm siteler" sorgusunda parent Yönetim adlarını <see cref="ITenantCatalogReader"/>
/// üzerinden lookup eder (cascade cache).
/// </para>
/// <para>
/// <b>v0.2.9 — IDbContextFactory'ye geçiş:</b> Blazor Server circuit'inde
/// component'ler (TenantSwitcher, RoleEditPage vb.) paralel olarak reader
/// çağırabiliyor; scoped DbContext "second operation started" hatası veriyordu.
/// Artık her metod factory'den taze bir DbContext üretip dispose eder.
/// </para>
/// </summary>
public sealed class MainCatalogReader : IMainCatalogReader
{
    private readonly ICacheStore _cache;
    private readonly IDbContextFactory<MainDbContext> _dbFactory;
    private readonly ITenantCatalogReader _tenantReader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public MainCatalogReader(
        ICacheStore cache,
        IDbContextFactory<MainDbContext> dbFactory,
        ITenantCatalogReader tenantReader)
    {
        _cache = cache;
        _dbFactory = dbFactory;
        _tenantReader = tenantReader;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CompanyListItem>> GetAllGlobalAsync(CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Company.AllGlobal,
            async ct =>
            {
                var tenants = await _tenantReader.GetAllActiveAsync(ct);
                var tenantNames = tenants.ToDictionary(t => t.Id, t => t.Name);

                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var rows = await db.Companies
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
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var rows = await db.Companies
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
            async ct =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                return await db.Companies
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new CompanyListItem(
                        c.Id, c.TenantId, null, c.UrlCode, c.Name, c.LegalName, c.Vkn, c.Status))
                    .FirstOrDefaultAsync(ct);
            },
            CacheOptions.DetailMediumLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<CompanyDetail?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<CompanyDetail?>(
            CacheKeys.Company.DetailById(id),
            async ct =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                return await db.Companies
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new CompanyDetail(
                        c.Id, c.TenantId, c.UrlCode, c.Name, c.LegalName, c.Vkn, c.Email, c.Phone, c.Status))
                    .FirstOrDefaultAsync(ct);
            },
            CacheOptions.DetailMediumLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<CompanyDetail?> GetDetailByUrlCodeAsync(string urlCode, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<CompanyDetail?>(
            CacheKeys.Company.DetailByUrlCode(urlCode),
            async ct =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                return await db.Companies
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(c => c.UrlCode == urlCode)
                    .Select(c => new CompanyDetail(
                        c.Id, c.TenantId, c.UrlCode, c.Name, c.LegalName, c.Vkn, c.Email, c.Phone, c.Status))
                    .FirstOrDefaultAsync(ct);
            },
            CacheOptions.DetailMediumLived,
            cancellationToken);
}
