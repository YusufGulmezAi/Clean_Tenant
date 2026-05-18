namespace CleanTenant.Application.Common.Caching;

/// <summary>
/// <para>
/// Bir cache entry için TTL ve davranış ayarları. <see cref="ICacheStore"/>
/// üzerinden geçirilir. v0.2.3.d generic hybrid cache mimarisi.
/// </para>
/// </summary>
/// <param name="AbsoluteExpiration">
/// Entry'nin oluşturulma anından itibaren ne kadar süre sonra silineceği.
/// L1 (IMemoryCache) ve L2 (Redis) için aynı değer geçer. Zorunlu (cache stampede
/// önlemek için TTL'siz entry yasak).
/// </param>
/// <param name="SlidingExpiration">
/// Erişim oldukça sürenin yenilenmesi (her read TTL'i resetler). Null ise
/// absolute kullanılır. Genelde session-like nadir kullanım için.
/// </param>
/// <param name="Tags">
/// Cache invalidation gruplandırması — örn. <c>"tenants"</c> tag'iyle yazılmış
/// tüm entry'leri tek seferde silmek için. v1'de henüz pasif (key-based
/// invalidation aktif); Faz 1.5+ tag-based invalidation'da kullanılır.
/// </param>
public sealed record CacheOptions(
    TimeSpan AbsoluteExpiration,
    TimeSpan? SlidingExpiration = null,
    IReadOnlyList<string>? Tags = null)
{
    /// <summary>Genel sözlük listesi sorguları için 5 dk default.</summary>
    public static readonly CacheOptions ListShortLived = new(TimeSpan.FromMinutes(5));

    /// <summary>Single-entity detay sorguları için 10 dk default.</summary>
    public static readonly CacheOptions DetailMediumLived = new(TimeSpan.FromMinutes(10));

    /// <summary>Çok nadir değişen referans verileri için 30 dk.</summary>
    public static readonly CacheOptions ReferenceLongLived = new(TimeSpan.FromMinutes(30));
}
