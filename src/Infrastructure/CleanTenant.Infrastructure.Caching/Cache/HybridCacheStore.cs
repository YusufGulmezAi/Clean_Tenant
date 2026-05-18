using System.Collections.Concurrent;
using System.Text.Json;
using CleanTenant.Application.Common.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.Cache;

/// <summary>
/// <para>
/// <see cref="ICacheStore"/>'un L1 (in-process <see cref="IMemoryCache"/>) +
/// L2 (Redis) hybrid implementasyonu. v0.2.3.d generic cache mimarisi.
/// </para>
/// <para>
/// <b>Read</b>: L1 hit → return; miss → L2; miss → null.
/// <b>Write</b>: hem L1 hem L2'ye yaz.
/// <b>Invalidate</b>: L1 + L2 sil + pub/sub publish.
/// <b>Stampede</b>: <see cref="GetOrCreateAsync"/> içinde key-bazlı
/// <see cref="SemaphoreSlim"/> ile aynı key için eşzamanlı factory çağrısı engelli.
/// </para>
/// <para>
/// L1 key takibi <see cref="_l1Keys"/> ile yapılır (prefix invalidation için).
/// IMemoryCache'in default API'sinde key enumeration yok; biz kendimiz tutarız.
/// </para>
/// </summary>
public sealed class HybridCacheStore : ICacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly IMemoryCache _l1;
    private readonly ILogger<HybridCacheStore> _logger;
    private readonly string _instanceId;

    /// <summary>L1 key seti — RemoveByPrefix için enumeration kaynağı.</summary>
    private readonly ConcurrentDictionary<string, byte> _l1Keys = new();

    /// <summary>Stampede koruması — aynı key için eşzamanlı factory tek thread.</summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _stampedeLocks = new();

    /// <summary>DI bağımlılıklarını alır.</summary>
    public HybridCacheStore(
        IConnectionMultiplexer redis,
        IMemoryCache l1,
        ILogger<HybridCacheStore> logger,
        string instanceId)
    {
        _redis = redis;
        _l1 = l1;
        _logger = logger;
        _instanceId = instanceId;
    }

    /// <summary>Yayan instance id'si (pub/sub origin filtresi için).</summary>
    public string InstanceId => _instanceId;

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // L1
        if (_l1.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        // L2
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return default;
            }

            var deserialized = JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
            if (deserialized is not null)
            {
                // L1 backfill — TTL Redis'ten al
                var ttl = await db.KeyTimeToLiveAsync(key);
                SetL1(key, deserialized, ttl ?? TimeSpan.FromMinutes(5), null);
            }
            return deserialized;
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException or RedisConnectionException)
        {
            _logger.LogWarning(ex, "Redis L2 cache okuma başarısız: {Key}", key);
            return default; // graceful degradation — caller factory'i çağırır
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, CacheOptions options, CancellationToken cancellationToken = default)
    {
        SetL1(key, value, options.AbsoluteExpiration, options.SlidingExpiration);

        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await db.StringSetAsync(key, json, options.AbsoluteExpiration);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException or RedisConnectionException)
        {
            _logger.LogWarning(ex, "Redis L2 cache yazma başarısız: {Key}", key);
            // L1 yazıldı; L2 kayıp tolerable — bir sonraki read'de tekrar dener.
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        RemoveL1(key);

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);

            // Pub/sub: diğer instance'lar L1'lerinden silsin
            await PublishInvalidationAsync(CacheInvalidationMessage.TypeKey, key);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException or RedisConnectionException)
        {
            _logger.LogWarning(ex, "Redis L2 cache invalidation başarısız: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
    {
        // L1: kendi tuttuğumuz key tablosundan prefix-match
        var matched = _l1Keys.Keys.Where(k => k.StartsWith(keyPrefix, StringComparison.Ordinal)).ToList();
        foreach (var k in matched)
        {
            RemoveL1(k);
        }

        // L2: Redis SCAN ile prefix-match (KEYS yerine production-safe)
        try
        {
            var endpoints = _redis.GetEndPoints();
            var pattern = $"{keyPrefix}*";
            foreach (var ep in endpoints)
            {
                var server = _redis.GetServer(ep);
                if (!server.IsConnected) continue;

                var keys = server.Keys(pattern: pattern, pageSize: 1000).ToArray();
                if (keys.Length > 0)
                {
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync(keys);
                }
            }

            // Pub/sub: prefix message
            await PublishInvalidationAsync(CacheInvalidationMessage.TypePrefix, keyPrefix);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException or RedisConnectionException)
        {
            _logger.LogWarning(ex, "Redis L2 prefix invalidation başarısız: {Prefix}", keyPrefix);
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheOptions options,
        CancellationToken cancellationToken = default)
    {
        // Fast path: cache hit
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        // Stampede koruması: aynı key için tek factory çalıştır
        var sem = _stampedeLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(cancellationToken);
        try
        {
            // Double-check — başka thread doldurdu mu?
            cached = await GetAsync<T>(key, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            var value = await factory(cancellationToken);
            if (value is not null)
            {
                await SetAsync(key, value, options, cancellationToken);
            }
            return value!;
        }
        finally
        {
            sem.Release();
            // Eğer sem kullanılmıyorsa dictionary'den temizle (memory leak önle)
            if (sem.CurrentCount == 1)
            {
                _stampedeLocks.TryRemove(key, out _);
            }
        }
    }

    /// <summary>L1'i temizle (pub/sub subscriber tarafından dışarıdan çağrılır).</summary>
    internal void RemoveFromL1Only(string key)
    {
        RemoveL1(key);
    }

    /// <summary>L1'den prefix temizle (pub/sub subscriber).</summary>
    internal void RemovePrefixFromL1Only(string keyPrefix)
    {
        var matched = _l1Keys.Keys.Where(k => k.StartsWith(keyPrefix, StringComparison.Ordinal)).ToList();
        foreach (var k in matched)
        {
            RemoveL1(k);
        }
    }

    private void SetL1<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan? slidingExpiration)
    {
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration,
        };
        if (slidingExpiration is { } slide)
        {
            entryOptions.SlidingExpiration = slide;
        }
        // Eviction callback ile key tablosundan otomatik düş
        entryOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            if (evictedKey is string s)
            {
                _l1Keys.TryRemove(s, out _);
            }
        });

        _l1.Set(key, value, entryOptions);
        _l1Keys.TryAdd(key, 0);
    }

    private void RemoveL1(string key)
    {
        _l1.Remove(key);
        _l1Keys.TryRemove(key, out _);
    }

    private async Task PublishInvalidationAsync(string type, string value)
    {
        var msg = new CacheInvalidationMessage(type, value, _instanceId);
        var json = JsonSerializer.Serialize(msg, JsonOptions);
        var sub = _redis.GetSubscriber();
        await sub.PublishAsync(RedisChannel.Literal(CacheKeys.InvalidationChannel), json);
    }
}
