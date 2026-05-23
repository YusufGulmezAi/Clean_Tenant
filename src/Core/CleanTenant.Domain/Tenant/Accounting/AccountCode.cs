using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// TDHP hesap kodu — şirkete ait hesap planının tek bir düğümü.
/// 3 kademeli hiyerarşi: Ana (3 hane) → Yardımcı (6 hane) → Detay (9 hane).
/// </para>
/// <para>
/// <b>Yevmiye girişi:</b> Yalnızca <see cref="IsDetail"/> = true olan yaprak
/// hesaplara fiş satırı (<see cref="JournalLine"/>) yazılabilir.
/// </para>
/// <para>
/// <b>Enflasyon muhasebesi:</b> <see cref="IsMonetary"/> = false olan varlıklar
/// (maddi duran varlıklar vb.) enflasyon düzeltme fişlerinde ayrıca takip edilir.
/// </para>
/// </summary>
public sealed class AccountCode : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Hesabın ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Hesap kodu. Kademesine göre format:
    /// Ana = "100", Yardımcı = "100.001", Detay = "100.001.001".
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Üst hesabın kodu; ana hesaplarda null.
    /// Örn. "100.001.001" için ParentCode = "100.001".
    /// </summary>
    public string? ParentCode { get; set; }

    /// <summary>Hesap adı (örn. "Kasa", "Garanti TRY Kasa").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Opsiyonel açıklama metni.</summary>
    public string? Description { get; set; }

    /// <summary>Hesap kademesi: Main / Sub / Detail.</summary>
    public AccountLevel Level { get; set; }

    /// <summary>TDHP ana sınıfı — ilk hane ile örtüşür (1→CurrentAsset vb.).</summary>
    public AccountClass AccountClass { get; set; }

    /// <summary>Bilanço karakteri: aktif, pasif veya karma.</summary>
    public AccountType AccountType { get; set; }

    /// <summary>Hesabın kaynağı: standart TDHP şablonu mu, şirkete özgü mü.</summary>
    public AccountCodeSource Source { get; set; }

    /// <summary>Hesap kullanıma açık mı.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// true = yaprak hesap; bu hesaba doğrudan yevmiye satırı yazılabilir.
    /// false = özet hesap; yalnızca alt toplamları gösterir.
    /// </summary>
    public bool IsDetail { get; set; }

    /// <summary>
    /// Parasal hesap mı? TMS 29 enflasyon düzeltmesinde parasal olmayan
    /// varlıklar (maddi duran varlık vb.) ayrıca yeniden değerlenir.
    /// false = parasal olmayan (makine, bina vb.).
    /// </summary>
    public bool IsMonetary { get; set; }

    /// <summary>
    /// true = şirket bu hesabı silemez ve kodunu değiştiremez.
    /// TDHP zorunlu hesapları için standart şablondan devralınır.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Bu hesabın türetildiği <see cref="global::CleanTenant.Domain.LookUp.ChartOfAccountsTemplate"/> kodu;
    /// standart hesaplar için dolu, şirkete özgü hesaplar için null.
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// Parasal olmayan varlıklar için edinim tarihi; enflasyon düzeltme
    /// katsayısının (endeks oranı) hesaplanmasında kullanılır.
    /// </summary>
    public DateOnly? AcquisitionDate { get; set; }
}
