using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.Sessions;

/// <summary>
/// <para>
/// <see cref="CachedAuthSessionStore.ChannelName"/> Redis pub/sub channel'ını
/// dinler ve gelen revocation mesajına göre local L1 cache'ten ilgili
/// AuthSession'ı siler. Multi-instance L1 senkronizasyonu için zorunlu.
/// </para>
/// <para>
/// Origin instance kendi mesajını yok sayar (kendisi zaten L1'i senkron sildi).
/// </para>
/// </summary>
public sealed class AuthSessionInvalidationSubscriber : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly CachedAuthSessionStore _cache;
    private readonly ILogger<AuthSessionInvalidationSubscriber> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AuthSessionInvalidationSubscriber(
        IConnectionMultiplexer redis,
        CachedAuthSessionStore cache,
        ILogger<AuthSessionInvalidationSubscriber> logger)
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
                RedisChannel.Literal(CachedAuthSessionStore.ChannelName),
                (channel, value) => OnMessage(value));

            _logger.LogInformation(
                "AuthSession invalidation subscriber boot edildi (channel: {Channel}, instance: {Instance})",
                CachedAuthSessionStore.ChannelName, _cache.InstanceId);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuthSession invalidation subscriber boot edilemedi");
        }
    }

    private void OnMessage(RedisValue value)
    {
        if (value.IsNullOrEmpty) return;

        try
        {
            var msg = JsonSerializer.Deserialize<AuthSessionInvalidationMessage>(value.ToString(), JsonOptions);
            if (msg is null) return;

            if (string.Equals(msg.OriginInstanceId, _cache.InstanceId, StringComparison.Ordinal))
            {
                return; // kendi mesajımız
            }

            if (Guid.TryParseExact(msg.SessionIdHex, "N", out var sessionId))
            {
                _cache.RemoveFromL1Only(sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AuthSession invalidation mesajı işlenemedi");
        }
    }
}
