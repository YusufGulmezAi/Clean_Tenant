using CleanTenant.Domain.LookUp.Districts;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.LookUp.Provinces;

/// <summary>Türkiye'deki il (province) referans verisi. Sistem geneli sabit kütüphanesi.</summary>
public sealed class Province : BaseEntity, IAggregateRoot, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>İl adı (örn. İstanbul, Ankara). Max 100 karakter.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Opsiyonel plaka kodu (1-81 arası). Türkiye İç İşleri tarafından tanımlanmış.</summary>
    public int? PlateCode { get; set; }

    /// <summary>Bu ile ait ilçeler.</summary>
    public ICollection<District> Districts { get; set; } = [];
}
