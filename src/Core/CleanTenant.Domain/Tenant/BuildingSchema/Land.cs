using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// <para>
/// Tapu kadastro hiyerarşisindeki en üst birim (ada). Bir <see cref="Company"/>
/// (Site) bünyesinde tanımlanır; her site'nin en az bir Land'i olmalıdır.
/// </para>
/// <para>
/// Ada numarası bilinmiyorsa "0" değeri kullanılabilir (default "0/0" kuralı).
/// </para>
/// </summary>
public sealed class Land : BaseEntity, IAggregateRoot, IHasUrlCode, ITenantScoped
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bu Land'ın ait olduğu Site (Company).</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Ada adı veya numarası (örn. "123", "A", "0").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Sıralama değeri; raporlama ve görüntülemede dikkate alınır.</summary>
    public int SortOrder { get; set; }

    /// <summary>Bu Land'ın ait olduğu Site (navigation property).</summary>
    public Company Company { get; set; } = null!;

    /// <summary>Bu Land altında tanımlı parseller (navigation property).</summary>
    public ICollection<Parcel> Parcels { get; set; } = [];
}
