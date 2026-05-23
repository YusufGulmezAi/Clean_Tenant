namespace CleanTenant.Application.Common.Caching;

/// <summary>
/// <para>
/// Domain-aware cache invalidation. Tenant/Company CRUD handler'ları
/// (v0.2.4+) write işleminden sonra ilgili invalidate metodunu çağırır.
/// </para>
/// <para>
/// Implementasyon: <see cref="ICacheStore.RemoveAsync"/> ve <see cref="ICacheStore.RemoveByPrefixAsync"/>
/// üzerinden çalışır; pub/sub ile multi-instance L1 senkronizasyonu sağlar.
/// </para>
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>Tek tenant'a ait cache entry'leri (by-id, list, vb.) temizler.</summary>
    Task InvalidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Tek company'ye ait cache entry'leri + parent tenant'ın company listesini temizler.</summary>
    Task InvalidateCompanyAsync(Guid companyId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Tüm Tenant cache'ini temizler (toplu işlemden sonra).</summary>
    Task InvalidateAllTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>Tüm Company cache'ini temizler (toplu işlemden sonra).</summary>
    Task InvalidateAllCompaniesAsync(CancellationToken cancellationToken = default);

    /// <summary>Tek role'e ait cache entry'leri (by-id, detail, permissions, vb.) temizler.</summary>
    Task InvalidateRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Tüm Role cache'ini temizler (toplu işlemden sonra).</summary>
    Task InvalidateAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>Permission cache'ini temizler (create/update/delete işlemlerinden sonra).</summary>
    Task InvalidatePermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm UserContexts cache'ini temizler. Tenant veya Company create/update/delete
    /// işlemlerinde çağrılır — AppBar Context Switcher'ın cache'lenmiş listesi
    /// (özellikle System kullanıcı için tüm Yönetim/Site kataloğu) hemen tazelensin diye.
    /// </summary>
    Task InvalidateAllUserContextsAsync(CancellationToken cancellationToken = default);
}
