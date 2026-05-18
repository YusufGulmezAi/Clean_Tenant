using CleanTenant.Domain.LookUp.Neighborhoods;
using CleanTenant.Domain.LookUp.Provinces;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.LookUp.Districts;

/// <summary>Türkiye'deki ilçe (district) referans verisi. Her ilçe bir ile bağlıdır.</summary>
public sealed class District : BaseEntity, IAggregateRoot, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>İlçe adı (örn. Beyoğlu, Çankaya). Max 100 karakter.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Bağlı olduğu il ID'si. Foreign key.</summary>
    public Guid ProvinceId { get; set; }

    /// <summary>Bağlı olduğu il entity'si (navigation property).</summary>
    public Province Province { get; set; } = null!;

    /// <summary>Bu ilçeye ait mahalleler.</summary>
    public ICollection<Neighborhood> Neighborhoods { get; set; } = [];
}
