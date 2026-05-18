using CleanTenant.Domain.LookUp.Districts;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.LookUp.Neighborhoods;

/// <summary>Türkiye'deki mahalle (neighborhood) referans verisi. Her mahalle bir ilçeye bağlıdır.</summary>
public sealed class Neighborhood : BaseEntity, IAggregateRoot, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Mahalle adı (örn. Taksim, Kızılay). Max 100 karakter.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Bağlı olduğu ilçe ID'si. Foreign key.</summary>
    public Guid DistrictId { get; set; }

    /// <summary>Bağlı olduğu ilçe entity'si (navigation property).</summary>
    public District District { get; set; } = null!;
}
