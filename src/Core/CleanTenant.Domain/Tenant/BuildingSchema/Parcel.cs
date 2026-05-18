using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// <para>
/// Bir <see cref="Block"/> (Ada) altındaki kadastro parseli.
/// Her Parcel'in en az bir <see cref="Building"/>'i tanımlanmalıdır.
/// </para>
/// <para>
/// Parsel numarası bilinmiyorsa "0" kullanılabilir.
/// </para>
/// </summary>
public sealed class Parcel : BaseEntity, IAggregateRoot, IHasUrlCode, ITenantScoped
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bağlı olduğu Ada (Block).</summary>
    public Guid BlockId { get; set; }

    /// <summary>Parsel adı veya numarası (örn. "45", "B-1", "0").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Sıralama değeri.</summary>
    public int SortOrder { get; set; }

    /// <summary>Bu Parcel'in ait olduğu Block (navigation property).</summary>
    public Block Block { get; set; } = null!;
    /// <summary>Bu Parcel'de inşa edilen binalar (navigation property).</summary>
    public ICollection<Building> Buildings { get; set; } = [];
}
