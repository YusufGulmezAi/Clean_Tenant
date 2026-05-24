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

    // ── v0.2.11.d — Adres (LookUp FK'ları), İletişim, Sözleşme ──

    /// <summary>Adres: bağlı il (LookUp.Provinces).</summary>
    public Guid? ProvinceId { get; set; }

    /// <summary>Adres: bağlı ilçe (LookUp.Districts).</summary>
    public Guid? DistrictId { get; set; }

    /// <summary>Adres: bağlı mahalle (LookUp.Neighborhoods).</summary>
    public Guid? NeighborhoodId { get; set; }

    /// <summary>İletişim kişisi adı-soyadı.</summary>
    public string? ContactPerson { get; set; }

    /// <summary>İletişim e-postası.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>İletişim telefonu.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>Sözleşme başlangıç tarihi.</summary>
    public DateOnly? ContractStartDate { get; set; }

    /// <summary>Sözleşme bitiş tarihi.</summary>
    public DateOnly? ContractEndDate { get; set; }

    /// <summary>Devir için verilen ek süre (gün).</summary>
    public int? TransitionGraceDays { get; set; }

    /// <summary>Yönetim durumu (yalnız Sistem scope Edit modunda değiştirilebilir).</summary>
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    // ── Hesap kilitleme politikası (Güvenlik tab) ──

    /// <summary>Hesap kilitleme aktif mi? Varsayılan true.</summary>
    public bool LockoutEnabled { get; set; } = true;

    /// <summary>Kilit için gereken ardışık hatalı deneme sayısı. Varsayılan 5.</summary>
    public int LockoutMaxFailedAttempts { get; set; } = 5;

    /// <summary>Kilit süresi (dakika). Varsayılan 15.</summary>
    public int LockoutDurationMinutes { get; set; } = 15;
}
