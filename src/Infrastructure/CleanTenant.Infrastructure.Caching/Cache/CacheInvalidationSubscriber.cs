using System.Text.Json;
using CleanTenant.Application.Common.Caching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.Cache;

/// <summary>
/// <para>
/// Redis pub/sub channel <c>cleantenant:v1:cache-invalidate</c>'i dinler ve
/// gelen invalidation mesajına göre yerel L1 cache'ten ilgili key/prefix'i siler.
/// Multi-instance L1 senkronizasyonu için zorunlu.
/// </para>
/// <para>
/// Origin instance kendi mesajını dinlerse görmezden gelir (kendisi zaten L1'i
/// senkron sildi).
/// </para>
/// </summary>
public sealed class CacheInvalidationSubscriber : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly HybridCacheStore _cache;
    private readonly ILogger<CacheInvalidationSubscriber> _logger;

    /// <summary>DI bağımlılıklarını alır. <paramref name="cache"/> concrete
    /// <see cref="HybridCacheStore"/> — internal L1-only metodlara erişim için.</summary>
    public CacheInvalidationSubscriber(
        IConnectionMultiplexer redis,
        HybridCacheStore cache,
        ILogger<CacheInvalidationSubscriber> logger)
    {
        _redis = redis;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var sub = _redis.GetSubscriber();
            await sub.SubscribeAsync(
                RedisChannel.Literal(CacheKeys.InvalidationChannel),
                (channel, value) => OnMessage(value));

            _logger.LogInformation("Cache invalidation subscriber boot edildi (channel: {Channel}, instance: {Instance})",
                CacheKeys.InvalidationChannel, _cache.InstanceId);

            // Stoppingtoken iptal olana kadar bekle
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache invalidation subscriber boot edilemedi");
        }
    }

    private void OnMessage(RedisValue value)
    {
        if (value.IsNullOrEmpty) return;

        try
        {
            var msg = JsonSerializer.Deserialize<CacheInvalidationMessage>(value.ToString(), JsonOptions);
            if (msg is null) return;

            // Origin instance kendi mesajını işlemesin (kendisi zaten L1'i sildi)
            if (string.Equals(msg.OriginInstanceId, _cache.InstanceId, StringComparison.Ordinal))
            {
                return;
            }

            if (string.Equals(msg.Type, CacheInvalidationMessage.TypeKey, StringComparison.Ordinal))
            {
                _cache.RemoveFromL1Only(msg.Value);
            }
            else if (string.Equals(msg.Type, CacheInvalidationMessage.TypePrefix, StringComparison.Ordinal))
            {
                _cache.RemovePrefixFromL1Only(msg.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalidation mesajı işlenemedi");
        }
    }
}
