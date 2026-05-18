namespace CleanTenant.Application.Common.Caching;

/// <summary>
/// <para>
/// <see cref="CacheableAttribute"/> üzerinden seçilen TTL preset'i. Enum
/// kullanma sebebi: attribute'da <see cref="TimeSpan"/> doğrudan literal olarak
/// taşınamıyor (compile-time constant değil). Preset enum bunu sarar.
/// </para>
/// </summary>
public enum CacheTtlPreset
{
    /// <summary>5 dakika — liste sorguları.</summary>
    ListShortLived,

    /// <summary>10 dakika — tek entity detayı.</summary>
    DetailMediumLived,

    /// <summary>30 dakika — nadir değişen referans verisi.</summary>
    ReferenceLongLived,
}

/// <summary>
/// <see cref="CacheTtlPreset"/> → <see cref="CacheOptions"/> dönüşüm helper'ı.
/// </summary>
public static class CacheTtlPresetExtensions
{
    /// <summary>Preset enum değerini <see cref="CacheOptions"/>'a çevirir.</summary>
    public static CacheOptions ToOptions(this CacheTtlPreset preset) => preset switch
    {
        CacheTtlPreset.ListShortLived => CacheOptions.ListShortLived,
        CacheTtlPreset.DetailMediumLived => CacheOptions.DetailMediumLived,
        CacheTtlPreset.ReferenceLongLived => CacheOptions.ReferenceLongLived,
        _ => CacheOptions.ListShortLived,
    };
}
