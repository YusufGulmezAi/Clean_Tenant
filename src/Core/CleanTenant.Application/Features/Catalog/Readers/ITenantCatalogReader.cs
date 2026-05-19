namespace CleanTenant.Application.Features.Catalog.Readers;

/// <summary>
/// <para>
/// Tenant (Yönetim) referans verisi için cache-aware read soyutlaması.
/// Implementasyon hybrid cache (L1 in-process + L2 Redis) üzerinden çalışır;
/// cache miss'te Catalog DB'ye fallback.
/// </para>
/// <para>
/// MediatR Query pipeline'ından bağımsız — sayfa component'leri doğrudan inject
/// eder. (MediatR <c>CachingBehavior</c> pipeline opsiyonu Faz 1.5+ için ayrı
/// bir katman.)
/// </para>
/// </summary>
public interface ITenantCatalogReader
{
    /// <summary>
    /// Tüm <b>Active</b> tenant'ları döner (ad'a göre sıralı). Cache TTL
    /// <c>CacheOptions.ListShortLived</c> (5 dk). Tenant CRUD handler'ları
    /// invalidate çağırır.
    /// </summary>
    Task<IReadOnlyList<TenantListItem>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Id ile tek tenant. Cache TTL <c>DetailMediumLived</c> (10 dk).</summary>
    Task<TenantListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>UrlCode ile tek tenant. Cache TTL <c>DetailMediumLived</c> (10 dk).</summary>
    Task<TenantListItem?> GetByUrlCodeAsync(string urlCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Düzenleme formu için tam detay — <see cref="TenantDetail"/>. Cache TTL
    /// <c>DetailMediumLived</c> (10 dk). Tenant CRUD'da invalidate edilir.
    /// </summary>
    Task<TenantDetail?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>UrlCode (9 karakter Base58) ile tam detay (v0.2.9 — UI rotalarında
    /// GUID yerine UrlCode kullanılır). Cache TTL <c>DetailMediumLived</c> (10 dk).</summary>
    Task<TenantDetail?> GetDetailByUrlCodeAsync(string urlCode, CancellationToken cancellationToken = default);
}
