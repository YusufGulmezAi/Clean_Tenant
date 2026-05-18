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
}
