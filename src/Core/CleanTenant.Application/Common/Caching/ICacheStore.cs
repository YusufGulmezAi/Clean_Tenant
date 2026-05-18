namespace CleanTenant.Application.Common.Caching;

/// <summary>
/// <para>
/// Generic hybrid cache soyutlaması. Implementasyon: L1 in-process
/// (<c>IMemoryCache</c>) + L2 Redis. Pub/sub ile multi-instance L1
/// senkronizasyonu sağlar.
/// </para>
/// <para>
/// <b>Read</b> akışı: L1 hit → return; L1 miss → L2 lookup → bulursa L1'e
/// backfill → return; L2 miss → null (caller factory çağırır veya
/// <see cref="GetOrCreateAsync"/> kullanır).
/// </para>
/// <para>
/// <b>Write</b> akışı: hem L1 hem L2'ye paralel yazım.
/// </para>
/// <para>
/// <b>Invalidate</b>: L1 + L2 sil + Redis pub/sub message → diğer instance'lar
/// kendi L1'lerini siler.
/// </para>
/// </summary>
public interface ICacheStore
{
    /// <summary>Cache'ten entry'i okur. Bulunamazsa <c>default</c>.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Entry'i set'ler (L1 + L2). Mevcut varsa üstüne yazılır.</summary>
    Task SetAsync<T>(string key, T value, CacheOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Entry'i siler (L1 + L2 + pub/sub). Yoksa sessiz geçer.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen prefix ile başlayan tüm key'leri siler. Redis SCAN ile çalışır
    /// (KEYS değil — production-safe). L1 tarafında local key tablosu üzerinden
    /// prefix match yapılır. Pub/sub ile diğer instance'lar haberdar olur.
    /// </summary>
    Task RemoveByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache hit → değeri döndür. Miss → <paramref name="factory"/> çalıştır,
    /// sonucu cache'e yaz, döndür. <c>SemaphoreSlim</c> ile cache stampede
    /// koruması var (aynı anda aynı key için tek factory).
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheOptions options,
        CancellationToken cancellationToken = default);
}
