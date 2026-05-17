using CleanTenant.Application.Common.MultiTenancy;
using CleanTenant.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CleanTenant.Infrastructure.Persistence.MultiTenancy;

/// <summary>
/// <para>
/// Hibrit multi-tenancy için <see cref="ITenantConnectionFactory"/>
/// implementasyonu. Catalog DB'den tenant lookup yapar; tenant dedicated DB
/// kullanıyorsa <c>TenantConnection</c>'dan, kullanmıyorsa shared Main DB
/// connection string'inden (konfigürasyondan) döner.
/// </para>
/// <para>
/// <b>Cache:</b> 5 dakika TTL ile in-memory. Tenant dedicated mode'a geçerse
/// veya bağlantı rotate edilirse TTL doğal olarak invalidate eder.
/// </para>
/// <para>
/// <b>v0.1.4'te plaintext:</b> <c>TenantConnection.ConnectionStringEncrypted</c>
/// şu an düz metin saklanıyor; v0.1.5'te DataProtection API ile şifrelenecek
/// ve burası decrypt çağrısı eklenecek.
/// </para>
/// </summary>
public sealed class TenantConnectionFactory : ITenantConnectionFactory
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "tenant-conn:";

    private readonly CatalogDbContext _catalog;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    /// <summary>Gerekli bağımlılıkları DI'dan alır.</summary>
    public TenantConnectionFactory(
        CatalogDbContext catalog,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _catalog = catalog;
        _cache = cache;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<string> GetMainConnectionStringAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyPrefix + tenantId.ToString("N");
        if (_cache.TryGetValue<string>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var tenant = await _catalog.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.Id, t.HasDedicatedDatabase })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                $"Tenant bulunamadı: {tenantId}");

        string connectionString;
        if (tenant.HasDedicatedDatabase)
        {
            connectionString = await _catalog.TenantConnections
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId && c.IsActive)
                .Select(c => c.ConnectionStringEncrypted)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Tenant {tenantId} dedicated DB modunda ama aktif TenantConnection kaydı yok.");

            // v0.1.5'te burada DataProtection ile decrypt yapılacak.
        }
        else
        {
            connectionString = _configuration.GetConnectionString("Main")
                ?? throw new InvalidOperationException(
                    "Shared Main DB bağlantı string'i 'ConnectionStrings:Main' bulunamadı.");
        }

        _cache.Set(cacheKey, connectionString, CacheDuration);
        return connectionString;
    }
}
