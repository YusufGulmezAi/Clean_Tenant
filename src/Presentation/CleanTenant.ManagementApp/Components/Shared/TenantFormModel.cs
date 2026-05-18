using CleanTenant.Domain.Identity.Tenants;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// <para>
/// <see cref="TenantForm"/> bileşeninin view-model'i. Üç mod için ortak alan
/// kümesi taşır; mod'a göre bazıları okunur-değiştirilemez render edilir
/// (form bileşeni karar verir) ya da submit'te yok sayılır (Application
/// command'leri zaten yalnız ilgili alanları alır).
/// </para>
/// <list type="bullet">
///   <item><see cref="TenantFormMode.Create"/> → tüm alanlar + Sorumlu Yönetici bloğu.</item>
///   <item><see cref="TenantFormMode.Edit"/> → Yönetim alanları (Sistem operatör).</item>
///   <item><see cref="TenantFormMode.Settings"/> → yalnız Name / LegalName / Address / AllowSystemWriteAccess
///   düzenlenebilir; kimlik / BillingTier / dedicated DB read-only.</item>
/// </list>
/// </summary>
public sealed class TenantFormModel
{
    /// <summary>Yönetim adı (tekil, max 256).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Yasal ad (opsiyonel, max 512).</summary>
    public string? LegalName { get; set; }

    /// <summary>Kimlik tipi (VKN/TCKN/YKN).</summary>
    public LegalIdentityType LegalIdentityType { get; set; } = LegalIdentityType.Vkn;

    /// <summary>Kimlik numarası (tekil, tipe göre format).</summary>
    public string LegalIdentityNumber { get; set; } = string.Empty;

    /// <summary>Adres (opsiyonel, max 512).</summary>
    public string? Address { get; set; }

    /// <summary>Faturalama katmanı.</summary>
    public BillingTier BillingTier { get; set; } = BillingTier.Standard;

    /// <summary>Dedicated DB kullanılacak mı (Edit/Create — Sistem operatör).</summary>
    public bool HasDedicatedDatabase { get; set; }

    /// <summary>Sistem operatörünün bu Yönetim'de yazma izni var mı (Support Mode).</summary>
    public bool AllowSystemWriteAccess { get; set; } = true;

    /// <summary>Sorumlu Yönetici adı (yalnız Create).</summary>
    public string AdminFirstName { get; set; } = string.Empty;

    /// <summary>Sorumlu Yönetici soyadı (yalnız Create).</summary>
    public string AdminLastName { get; set; } = string.Empty;

    /// <summary>Sorumlu Yönetici e-postası (yalnız Create, tekil).</summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Sorumlu Yönetici telefonu — format <c>0(5XX) XXX-XX-XX</c> (yalnız Create).</summary>
    public string AdminPhone { get; set; } = string.Empty;
}
