using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// Gider kategorisi — bütçe kalemlerinin makro gruplaması ("Personel",
/// "Enerji", "Bakım", "Sigorta", "Yedek Akçe" vb.). TDHP hesap planından
/// ayrıdır; raporlama ve UI gruplaması için kullanılır.
/// </para>
/// <para>
/// Hiyerarşi: <see cref="ParentCategoryId"/> üzerinden ağaç yapısı. MVP'de
/// 2 seviye yeterli (Ana kategori → Alt kategori); daha derin hiyerarşi
/// engellenmez, ancak UI 2 seviye gösterir.
/// </para>
/// <para>
/// Şirket bazlıdır; her site kendi kategorilerini özelleştirebilir. Sistem
/// seed'i ile başlangıçta ortak bir şablon yüklenebilir (Wave 2+).
/// </para>
/// </summary>
public sealed class ExpenseCategory : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Kategorinin ait olduğu site.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Üst kategori; kök seviye için null.</summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>Kısa kod (örn. "PERS", "ENRJ"). (CompanyId, Code) benzersiz.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Görünen ad (örn. "Personel Giderleri").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Açıklama / not. Opsiyonel.</summary>
    public string? Description { get; set; }

    /// <summary>UI sıralama önceliği; aynı parent altında küçükten büyüğe.</summary>
    public int DisplayOrder { get; set; }
}
