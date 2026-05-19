namespace CleanTenant.Application.Features.Main.Readers;

/// <summary>
/// <para>
/// Main DB referans verisi (Company / Site) için cache-aware read soyutlaması.
/// Implementasyon hybrid cache + Main DB fallback. <see cref="GetAllGlobalAsync"/>
/// Sistem operatörü için tüm siteleri parent Yönetim adlarıyla birlikte döner;
/// <see cref="GetByTenantAsync"/> aktif Yönetim bağlamı için filtreli.
/// </para>
/// </summary>
public interface IMainCatalogReader
{
    /// <summary>
    /// Tüm site'leri (parent Yönetim adıyla denormalize) döner — Sistem operatörü
    /// içindir. <c>IgnoreQueryFilters</c> bypass'lı, çoklu tenant. Cache TTL
    /// <c>ListShortLived</c> (5 dk).
    /// </summary>
    Task<IReadOnlyList<CompanyListItem>> GetAllGlobalAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli Yönetim'in site'lerini döner. <c>TenantName</c> alanı opsiyonel
    /// (caller bağlamından zaten biliniyor — null olabilir). Cache TTL
    /// <c>ListShortLived</c> (5 dk).
    /// </summary>
    Task<IReadOnlyList<CompanyListItem>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Tek company id ile. Cache TTL <c>DetailMediumLived</c> (10 dk).</summary>
    Task<CompanyListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Düzenleme formu için tam detay — <see cref="CompanyDetail"/>. Cache TTL
    /// <c>DetailMediumLived</c> (10 dk). Company CRUD'da invalidate edilir.</summary>
    Task<CompanyDetail?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>UrlCode (9 karakter Base58) ile tam detay (v0.2.9.d — UI rotaları GUID
    /// yerine UrlCode kullanır). Cache TTL <c>DetailMediumLived</c> (10 dk).</summary>
    Task<CompanyDetail?> GetDetailByUrlCodeAsync(string urlCode, CancellationToken cancellationToken = default);
}
