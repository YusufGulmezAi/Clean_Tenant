using System.Text.Json;
using CleanTenant.Application.Common.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.Sessions;

/// <summary>
/// <para>
/// <see cref="IAuthSessionStore"/>'un L1 (in-process <see cref="IMemoryCache"/>)
/// decorator implementasyonu. <see cref="RedisAuthSessionStore"/>'u sarar;
/// L1 hit'lerde Redis roundtrip atlanır.
/// </para>
/// <para>
/// <b>Tasarım sınırları (security/correctness için):</b>
/// </para>
/// <list type="bullet">
///   <item><b>L1 TTL: 10 saniye.</b> Bir başka instance'tan revocation gelirse
///   pub/sub mesajına kadar geçen sürede stale erişim mümkün; TTL bunu üst sınır olarak kapatır.</item>
///   <item><b>Sliding <see cref="TouchAsync"/></b> her zaman Redis'e gider —
///   L1'i atlayamaz, çünkü Redis'in TTL'ini yenilemek server-side aksiyon.</item>
///   <item><b>Mutation</b> (<see cref="UpdateAsync"/>) Redis'e yazar ve <b>yeni
///   versiyon hemen L1'e</b> konur — diğer instance'ları pub/sub uyandırır.</item>
///   <item><b>Revocation</b> (<see cref="DeleteAsync"/>, <see cref="DeleteAllForUserAsync"/>)
///   L1'den siler ve pub/sub publish eder.</item>
/// </list>
/// <para>
/// Pub/sub channel: <see cref="ChannelName"/>. Format: <c>"revoke {sessionId}"</c>.
/// Subscriber <see cref="AuthSessionInvalidationSubscriber"/> tarafında yapılır.
/// </para>
/// </summary>
public sealed class CachedAuthSessionStore : IAuthSessionStore
{
    /// <summary>L1 cache giriş süresi. Revocation latency = TTL.</summary>
    public static readonly TimeSpan L1Ttl = TimeSpan.FromSeconds(10);

    /// <summary>Pub/sub channel adı — AuthSession invalidation için özel.</summary>
    public const string ChannelName = "cleantenant:v1:auth-session-invalidate";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RedisAuthSessionStore _inner;
    private readonly IMemoryCache _l1;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CachedAuthSessionStore> _logger;
    private readonly string _instanceId;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CachedAuthSessionStore(
        RedisAuthSessionStore inner,
        IMemoryCache l1,
        IConnectionMultiplexer redis,
        ILogger<CachedAuthSessionStore> logger,
        string instanceId)
    {
        _inner = inner;
        _l1 = l1;
        _redis = redis;
        _logger = logger;
        _instanceId = instanceId;
    }

    /// <summary>Yayan instance id'si — subscriber kendi mesajını yok sayar.</summary>
    public string InstanceId => _instanceId;

    /// <inheritdoc />
    public async Task StoreAsync(AuthSession session, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        await _inner.StoreAsync(session, ttl, cancellationToken);
        SetL1(session);
    }

    /// <inheritdoc />
    public async Task<AuthSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var l1Key = L1Key(sessionId);
        if (_l1.TryGetValue(l1Key, out AuthSession? cached) && cached is not null)
        {
            return cached;
        }

        var session = await _inner.GetAsync(sessionId, cancellationToken);
        if (session is not null)
        {
            SetL1(session);
        }
        return session;
    }

    /// <inheritdoc />
    public Task TouchAsync(Guid sessionId, TimeSpan newTtl, CancellationToken cancellationToken = default)
    {
        // Sliding TTL Redis-side aksiyon — L1'i atlayamaz. L1'deki LastActivity
        // 10 sn süreyle eski kalabilir (audit etkisi minimal).
        return _inner.TouchAsync(sessionId, newTtl, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AuthSession session, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        await _inner.UpdateAsync(session, ttl, cancellationToken);
        // Hemen güncel versiyonu L1'e koy
        SetL1(session);
        // Diğer instance'lar L1'lerini sıfırlasın → tekrar L2'den taze çekecekler
        await PublishRevocationAsync(session.SessionId);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(sessionId, userId, cancellationToken);
        _l1.Remove(L1Key(sessionId));
        await PublishRevocationAsync(sessionId);
    }

    /// <inheritdoc />
    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Önce sessionId listesini al ki L1'leri temizleyebilelim + pub/sub gönderebilelim
        var sessionIds = await _inner.GetActiveSessionIdsForUserAsync(userId, cancellationToken);

        await _inner.DeleteAllForUserAsync(userId, cancellationToken);

        foreach (var sid in sessionIds)
        {
            _l1.Remove(L1Key(sid));
        }

        // Pub/sub — diğer instance'lar her session için L1 invalidate etsin
        foreach (var sid in sessionIds)
        {
            await PublishRevocationAsync(sid);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> GetActiveSessionIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // User session set'i Redis-only (L1'de tutmuyoruz — değişkenlik yüksek)
        return _inner.GetActiveSessionIdsForUserAsync(userId, cancellationToken);
    }

    /// <summary>Pub/sub subscriber'ı bu metodla L1'den siler.</summary>
    internal void RemoveFromL1Only(Guid sessionId)
    {
        _l1.Remove(L1Key(sessionId));
    }

    private void SetL1(AuthSession session)
    {
        _l1.Set(L1Key(session.SessionId), session, L1Ttl);
    }

    private static string L1Key(Guid sessionId) => $"l1:auth-session:{sessionId:N}";

    private async Task PublishRevocationAsync(Guid sessionId)
    {
        try
        {
            var payload = JsonSerializer.Serialize(
                new AuthSessionInvalidationMessage(sessionId.ToString("N"), _instanceId),
                JsonOptions);
            var sub = _redis.GetSubscriber();
            await sub.PublishAsync(RedisChannel.Literal(ChannelName), payload);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException or RedisConnectionException)
        {
            _logger.LogWarning(ex, "AuthSession revocation publish başarısız: {SessionId}", sessionId);
            // L2 zaten silindi; pub/sub kayıp tolerable — diğer instance'lar TTL kadar bekler.
        }
    }
}

/// <summary>
/// AuthSession invalidation pub/sub mesajı. Subscriber bu mesajı alınca local L1'den siler.
/// </summary>
/// <param name="SessionIdHex">Silinecek session id (Guid "N" formatı — 32 hex).</param>
/// <param name="OriginInstanceId">Yayan instance — kendi mesajını tekrar işlemesin.</param>
public sealed record AuthSessionInvalidationMessage(string SessionIdHex, string OriginInstanceId);
