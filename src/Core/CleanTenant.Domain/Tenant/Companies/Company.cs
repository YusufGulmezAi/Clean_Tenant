using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Companies;

/// <summary>
/// <para>
/// Tenant'a ait şirket (legal entity). Faz 1'in ilk iş varlığı — Tenant
/// onboarding wizard'ı en az bir Company yaratır.
/// </para>
/// <para>
/// <b>Multi-tenancy:</b> <see cref="ITenantScoped"/> implementasyonu ile
/// Main DB'nin shared-mode'unda <c>tenant_id</c> kolonu üzerinden izole
/// edilir; EF Core global query filter otomatik uygular.
/// </para>
/// <para>
/// <b>URL'de görünür:</b> Yönetim panelinde <c>/companies/{urlCode}</c>
/// olarak adreslenir.
/// </para>
/// </summary>
public sealed class Company : BaseEntity, IAggregateRoot, IHasUrlCode, ITenantScoped
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Şirketin ait olduğu tenant kimliği (multi-tenancy filter).</summary>
    public Guid TenantId { get; set; }

    /// <summary>Şirketin görünür adı (citext, tenant içinde unique).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Yasal / ticari ad (faturalama + sözleşmeler için); opsiyonel.</summary>
    public string? LegalName { get; set; }

    /// <summary>Vergi Kimlik Numarası (10 hane); DB CHECK ile format dayatılır.</summary>
    public string? Vkn { get; set; }

    /// <summary>İletişim e-postası (opsiyonel).</summary>
    public string? Email { get; set; }

    /// <summary>İletişim telefonu (opsiyonel; serbest format Faz 1, E.164 sonra).</summary>
    public string? Phone { get; set; }

    /// <summary>Şirketin yaşam döngüsü durumu.</summary>
    public CompanyStatus Status { get; set; }

    /// <summary>Bu Company'e ait Ada kayıtları (navigation property).</summary>
    public ICollection<CleanTenant.Domain.Tenant.BuildingSchema.Land> Lands { get; set; } = [];
}
