using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Identity.Tenants;

/// <summary>
/// <para>
/// CleanTenant SaaS platformunun müşteri tenant aggregate kökü. Her tenant,
/// kendine ait Company, Building, Unit ve kullanıcı atamalarını taşıyan
/// bağımsız bir mantıksal kira birimidir.
/// </para>
/// <para>
/// <b>Multi-tenancy konumu:</b> <see cref="HasDedicatedDatabase"/> true ise
/// tenant'ın iş verisi ayrı bir Main DB'de yaşar (Enterprise tier);
/// <c>TenantConnection</c> entity'si bağlantı bilgisini taşır. Aksi takdirde
/// tüm tenant'lar paylaşılan Main DB'de TenantId kolonuyla ayrılır.
/// </para>
/// <para>
/// <b>URL'de görünür:</b> ManagementApp'te <c>/admin/tenants/{urlCode}</c>
/// olarak adreslenir; bu nedenle <see cref="IHasUrlCode"/> implement eder.
/// </para>
/// </summary>
public sealed class Tenant : BaseEntity, IAggregateRoot, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu (görünür tanımlayıcı).</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Tenant'ın görünür adı (UI'da ve operasyonel kayıtlarda kullanılır).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tenant'ın yasal / ticari adı (fatura ve sözleşmelerde kullanılır); opsiyonel.</summary>
    public string? LegalName { get; set; }

    /// <summary>Tenant'ın yaşam döngüsü durumu.</summary>
    public TenantStatus Status { get; set; }

    /// <summary>Tenant'ın faturalama katmanı.</summary>
    public BillingTier BillingTier { get; set; }

    /// <summary>
    /// True ise tenant'ın iş verisi ayrı bir Main DB'de saklanır
    /// (<see cref="TenantConnection"/> bağlantı bilgisini taşır).
    /// False ise paylaşılan Main DB içinde TenantId kolonuyla ayrılır.
    /// </summary>
    public bool HasDedicatedDatabase { get; set; }

    /// <summary>
    /// Dedicated DB tenant'ları için DB schema adı (örn. <c>tenant_acme</c>).
    /// Shared mode'da null.
    /// </summary>
    public string? DatabaseSchemaName { get; set; }
}
