using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.LookUp;

/// <summary>
/// <para>
/// TDHP hesap planı şablonu — Catalog DB'de saklanan sistem geneli referans verisi.
/// Yeni şirket oluşturulduğunda bu şablondan klonlanarak şirkete özgü
/// <see cref="Tenant.Accounting.AccountCode"/> kayıtları üretilir.
/// </para>
/// <para>
/// <b>Multi-tenancy:</b> LookUp entity'si olduğundan <c>ITenantScoped</c> ve
/// <c>IAggregateRoot</c> implement etmez; yalnızca <c>BaseEntity</c>'den türer.
/// </para>
/// <para>
/// <b>Güncelleme politikası:</b> Yasal mevzuat değişikliklerinde Sistem Admin
/// şablonu günceller; mevcut şirket hesap kodlarına otomatik yansımaz —
/// şirket bazlı güncelleme ayrı bir komutla tetiklenir.
/// </para>
/// </summary>
public sealed class ChartOfAccountsTemplate : BaseEntity
{
    /// <summary>
    /// Hesap kodu (örn. "100", "100.001", "100.001.001").
    /// Şablon içinde benzersizdir.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Üst hesabın kodu; ana hesaplarda null.</summary>
    public string? ParentCode { get; set; }

    /// <summary>Hesap adı (Türkçe, resmi TDHP terminolojisi).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Hesap kademesi: Main / Sub / Detail.</summary>
    public AccountLevel Level { get; set; }

    /// <summary>TDHP ana sınıfı (ilk hane ile örtüşür).</summary>
    public AccountClass AccountClass { get; set; }

    /// <summary>Bilanço karakteri: aktif, pasif veya karma.</summary>
    public AccountType AccountType { get; set; }

    /// <summary>
    /// true = şirket bu hesabı silemez ve kodunu değiştiremez.
    /// Zorunlu TDHP hesapları işaretlidir.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// true = yaprak hesap; şirkete aktarıldığında yevmiye girilebilir.
    /// </summary>
    public bool IsDetail { get; set; }

    /// <summary>
    /// Parasal hesap mı? TMS 29 enflasyon düzeltmesinde kullanılır.
    /// false = parasal olmayan (maddi duran varlık vb.).
    /// </summary>
    public bool IsMonetary { get; set; }

    /// <summary>Şablondaki görüntüleme sırası (raporlama ve liste için).</summary>
    public int DisplayOrder { get; set; }
}
