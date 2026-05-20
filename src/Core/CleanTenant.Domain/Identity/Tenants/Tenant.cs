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

    /// <summary>
    /// <para>
    /// Sistem kullanıcılarının (SaaS operatör personeli) bu Yönetim ve altındaki
    /// Site'lere destek amaçlı erişiminde <b>yazma yetkisi</b> ile girip
    /// giremeyeceğini belirler. v0.2.3.c "Support Mode v2" modeli:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>true</c> (default): Sistem kullanıcı doğrudan WriteEnabled modunda girer; her aksiyon <see cref="Support.SupportSession"/>'a kaydedilir.</item>
    ///   <item><c>false</c>: Sistem kullanıcı yalnızca ReadOnly görüntüleme; yazma denemesi <c>AUTH-SUPPORT-WRITE-DENIED</c> ile reddedilir.</item>
    /// </list>
    /// <para>
    /// Yönetim Admin (TenantAdmin) parametre değiştirme akışı mail link onayı
    /// ile korunur — token imzalı, tek kullanımlık, TTL'li.
    /// </para>
    /// </summary>
    public bool AllowSystemWriteAccess { get; set; } = true;

    /// <summary>
    /// <para>
    /// Yönetim'in yasal kimlik tipi. <see cref="LegalIdentityType.Vkn"/>,
    /// <see cref="LegalIdentityType.Tckn"/> veya <see cref="LegalIdentityType.Ykn"/>
    /// — mutually exclusive. v0.2.4.b.
    /// </para>
    /// </summary>
    public LegalIdentityType LegalIdentityType { get; set; }

    /// <summary>
    /// <para>
    /// Yönetim'in yasal kimlik numarası. Tipe göre format:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>VKN</b>: 10 hane, ilk hane 1-9 (^[1-9]\d{9}$).</item>
    ///   <item><b>TCKN</b>: 11 hane, ilk hane 1-9 (^[1-9]\d{10}$).</item>
    ///   <item><b>YKN</b>: 11 hane, "99" ile başlar (^99\d{9}$).</item>
    /// </list>
    /// <para>
    /// DB seviyesinde CHECK constraint ile format zorlanır. Global tekildir
    /// (Catalog DB'de unique index).
    /// </para>
    /// </summary>
    public string LegalIdentityNumber { get; set; } = string.Empty;

    /// <summary>
    /// Yönetim'in posta adresi (opsiyonel). Faturalama ve resmi yazışma için.
    /// Max 512 karakter.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>Adres: bağlı il (LookUp.Provinces FK). v0.2.11.b'de eklendi.</summary>
    public Guid? ProvinceId { get; set; }

    /// <summary>Adres: bağlı ilçe (LookUp.Districts FK). v0.2.11.b'de eklendi.</summary>
    public Guid? DistrictId { get; set; }

    /// <summary>Adres: bağlı mahalle (LookUp.Neighborhoods FK). v0.2.11.b'de eklendi.</summary>
    public Guid? NeighborhoodId { get; set; }

    /// <summary>
    /// İletişim kişisi adı-soyadı (operasyonel temas). Sorumlu Yönetici User'ından
    /// ayrı tutulur — User kimlik için, ContactPerson iş iletişimi için.
    /// Max 200 karakter.
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>İletişim e-postası (genel kurumsal). Max 256 karakter.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// İletişim telefonu (genel kurumsal). Max 32 karakter — uluslararası
    /// formatlara izin verir.
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Sözleşmenin başlangıç tarihi (gün hassasiyetinde). Faturalama döngüsü
    /// hesabında ve tahsilat takvimi/uyarılarda kullanılır.
    /// </summary>
    public DateOnly? ContractStartDate { get; set; }

    /// <summary>
    /// Sözleşmenin bitiş tarihi (gün hassasiyetinde). Süresiz sözleşmeler için
    /// null kalır. Yenileme/uyarı akışlarının kaynak alanı.
    /// </summary>
    public DateOnly? ContractEndDate { get; set; }

    /// <summary>
    /// Sözleşme bitişi sonrası devir/kapanış için verilen ek süre (gün).
    /// Veri taşıma, son fatura gibi işlemler bu pencerede tamamlanır.
    /// Null → ek süre tanımlanmamış.
    /// </summary>
    public int? TransitionGraceDays { get; set; }
}
