namespace CleanTenant.Application.Common.MultiTenancy;

/// <summary>
/// <para>
/// Hibrit multi-tenancy için belirli bir tenant'ın Main DB bağlantı dizgesini
/// çözen servis. Application katmanı bu arabirim üzerinden tenant-bazlı
/// bağlantı alır; concrete implementasyon Infrastructure.Persistence'tadır.
/// </para>
/// <para>
/// <b>Davranış:</b>
/// <list type="bullet">
///   <item>Tenant'ın <c>HasDedicatedDatabase</c>'i true ise → tenant'a özel
///   bağlantı string'i (Catalog'taki <c>TenantConnection</c> kaydından) döner.</item>
///   <item>Aksi takdirde → paylaşılan Main DB bağlantı string'i (config'ten) döner.</item>
/// </list>
/// </para>
/// <para>
/// Sonuçlar belirli bir süre (varsayılan 5 dakika) cache'lenir; tenant
/// kaydı değişirse cache invalidation manuel veya TTL doğal olarak.
/// </para>
/// </summary>
public interface ITenantConnectionFactory
{
    /// <summary>
    /// Verilen tenant için Main DB bağlantı string'ini döner.
    /// </summary>
    /// <param name="tenantId">Hedef tenant kimliği.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>Bağlantı string'i. Tenant bulunamazsa <see cref="InvalidOperationException"/>.</returns>
    Task<string> GetMainConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
