using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// Bütçe kalemi tanımı (line definition). Bütçe versiyonundan bağımsız;
/// her versiyon kendi <see cref="BudgetLineVersion"/>'ı üzerinden bu kaleme
/// bir tutar/formül/plan atar.
/// </para>
/// <para>
/// Örnekler: "Asansör Bakımı", "Apartman Elektriği", "Personel Maaşları",
/// "Yedek Akçe (KMK m.20)". Şirket bazlıdır; bir site kendi listesini
/// özelleştirebilir.
/// </para>
/// <para>
/// <b>Aggregate root:</b> Versiyonlar bütçeye, tanımlar kendi başına yönetilir.
/// Bütçe versiyonu değişse bile tanım stabil kalır; bu sayede versiyonlar
/// arası karşılaştırma "aynı kalemde tutar artmış" mantığı doğru çalışır.
/// </para>
/// </summary>
public sealed class BudgetLine : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Kalemin ait olduğu site.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gider kategorisi (gruplama için zorunlu).</summary>
    public Guid ExpenseCategoryId { get; set; }

    /// <summary>
    /// Bağlı TDHP hesap kodu (opsiyonel). Set edilirse tahakkuk yevmiye fişi
    /// bu hesaba yazılır. Null ise sadece bütçe takibi yapılır, muhasebe yansıması
    /// manuel.
    /// </summary>
    public Guid? AccountCodeId { get; set; }

    /// <summary>Kısa kod (örn. "ASBKM-01"). (CompanyId, Code) benzersiz.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Görünen ad (örn. "Asansör Bakımı"). Maliklere bu ad görünür.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Açıklama / not. Opsiyonel.</summary>
    public string? Description { get; set; }

    /// <summary>Aktif mi; pasif kalemler taslak bütçelerde seçilemez.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// UI / rapor sıralama. Aynı kategori altında küçükten büyüğe.
    /// </summary>
    public int DisplayOrder { get; set; }
}
