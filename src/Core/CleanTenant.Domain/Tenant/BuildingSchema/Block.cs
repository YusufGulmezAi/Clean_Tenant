using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// <para>
/// Bir <see cref="Building"/> (Yapı) bünyesindeki blok/kule (örn. "A Blok", "B Blok").
/// Büyük sitelerde bir yapı birden çok bloktan oluşabilir; küçük yapılarda blok olmayabilir.
/// </para>
/// <para>
/// BB (<see cref="Unit"/>) ya doğrudan bir Building'e ya da bir Block'a bağlıdır.
/// Block varsa KapıNo (Number) bu Block içinde unique olmalıdır; yoksa Building içinde unique.
/// </para>
/// </summary>
public sealed class Block : BaseEntity, IAggregateRoot, IHasUrlCode, ITenantScoped
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bu Block'un ait olduğu Yapı.</summary>
    public Guid BuildingId { get; set; }

    /// <summary>Blok adı veya kodu (örn. "A Blok", "B", "1. Kule").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Sıralama değeri; listeleme ve raporlarda kullanılır.</summary>
    public int SortOrder { get; set; }

    /// <summary>Bu Block'un ait olduğu Yapı (navigation property).</summary>
    public Building Building { get; set; } = null!;

    /// <summary>Bu Block'taki bağımsız bölümler (navigation property).</summary>
    public ICollection<Unit> Units { get; set; } = [];
}
