using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Maliyet merkezi — giderlerin ve gelirlerin hangi birimin üzerine
/// yüklendiğini gösteren düz liste yapısı.
/// </para>
/// <para>
/// Hiyerarşi yoktur; gerektiğinde kod ön eki (örn. "10-A1") ile
/// gruplama raporlama katmanında yapılır.
/// </para>
/// </summary>
public sealed class CostCenter : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Maliyet merkezinin ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Kısa kod (örn. "10", "20", "30"); şirket içinde benzersiz.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Maliyet merkezi adı (örn. "Yönetim", "Teknik Servis").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Opsiyonel açıklama metni.</summary>
    public string? Description { get; set; }

    /// <summary>Maliyet merkezi aktif mi; pasif merkeze yeni fiş satırı yazılamaz.</summary>
    public bool IsActive { get; set; } = true;
}
