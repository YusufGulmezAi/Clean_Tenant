using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// Bir <see cref="Parcel"/> üzerinde inşa edilmiş bina/blok.
/// Yapının bağımsız bölümleri (<see cref="Unit"/>) bu entity altında tanımlanır.
/// </summary>
public sealed class Building : BaseEntity, IAggregateRoot, IHasUrlCode, ITenantScoped
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bağlı olduğu Parsel.</summary>
    public Guid ParcelId { get; set; }

    /// <summary>Yapı adı veya blok kodu (örn. "A Blok", "1. Bina").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Belediye yapı ruhsatı / kayıt numarası (opsiyonel).</summary>
    public string? MunicipalNo { get; set; }

    /// <summary>Yapının imar/tapu kullanım tipi.</summary>
    public BuildingType Type { get; set; }

    /// <summary>Sıralama değeri.</summary>
    public int SortOrder { get; set; }

    /// <summary>Bu Building'in inşa edildiği Parcel (navigation property).</summary>
    public Parcel Parcel { get; set; } = null!;
    /// <summary>Bu Building'deki bağımsız bölümler (navigation property).</summary>
    public ICollection<Unit> Units { get; set; } = [];

    /// <summary>Bu Building'deki bloklar/kuleler (opsiyonel; navigation property).</summary>
    public ICollection<Block> Blocks { get; set; } = [];
}
