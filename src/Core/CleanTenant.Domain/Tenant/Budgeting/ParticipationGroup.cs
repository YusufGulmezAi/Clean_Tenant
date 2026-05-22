using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// Katılım grubu — belirli bir giderin sadece belirli Bağımsız Bölümlere
/// dağıtılması gerektiğinde tanımlanır. Örnek: "Havuz Kullanıcıları",
/// "Ticari Birimler", "Sosyal Tesis Kullanıcıları".
/// </para>
/// <para>
/// Üyelik <see cref="UnitParticipationGroup"/> junction tablosu üzerinden
/// many-to-many olarak tutulur; bir BB birden çok grupta olabilir.
/// </para>
/// <para>
/// MVP'de dağıtım sırasında her <see cref="BudgetLineVersion"/> en fazla
/// bir <see cref="ParticipationGroup"/>'a bağlanır (1:1). Bir kalem için
/// birden çok grup arasında split dağıtım Wave 2+'a bırakıldı (o zaman
/// <c>ExpenseShareGroup</c> ara entity'si gelir).
/// </para>
/// </summary>
public sealed class ParticipationGroup : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Grubun ait olduğu site.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Kısa kod (örn. "HAVZ"). (CompanyId, Code) benzersiz.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Görünen ad (örn. "Havuz Kullanıcıları").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Açıklama / not. Opsiyonel.</summary>
    public string? Description { get; set; }

    /// <summary>Aktif mi; pasif gruplar yeni dağıtımlara seçilemez.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Üye Bağımsız Bölümler.</summary>
    public ICollection<UnitParticipationGroup> Memberships { get; set; } = [];
}
