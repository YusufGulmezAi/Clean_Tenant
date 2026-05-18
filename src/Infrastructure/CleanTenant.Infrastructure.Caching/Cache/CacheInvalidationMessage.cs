namespace CleanTenant.Infrastructure.Caching.Cache;

/// <summary>
/// <para>
/// Redis pub/sub üzerinden taşınan invalidation mesajı. Bir instance bir key'i
/// veya prefix'i invalidate ettiğinde diğer instance'ların L1 (IMemoryCache)
/// kopyalarından silinmesini sağlar.
/// </para>
/// </summary>
/// <param name="Type">"key" (tek anahtar) veya "prefix" (toplu).</param>
/// <param name="Value">Silinecek key veya prefix.</param>
/// <param name="OriginInstanceId">Mesajı yayan instance'ın id'si (kendi mesajını tekrar işlemesin).</param>
public sealed record CacheInvalidationMessage(string Type, string Value, string OriginInstanceId)
{
    /// <summary>Tek key invalidation.</summary>
    public const string TypeKey = "key";

    /// <summary>Prefix tabanlı toplu invalidation.</summary>
    public const string TypePrefix = "prefix";
}
