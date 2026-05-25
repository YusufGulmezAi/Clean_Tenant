using System.Text.Json;
using CleanTenant.Application.Common.Auth;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.Sessions;

/// <summary>
/// <para>
/// <see cref="IAuthSessionStore"/>'un StackExchange.Redis ile gerçekleştirilmiş
/// implementasyonu. Session JSON olarak saklanır; kullanıcı index'i Redis set
/// olarak tutulur (toplu revocation hızlı).
/// </para>
/// </summary>
public sealed class RedisAuthSessionStore : IAuthSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly SessionKeyBuilder _keys;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RedisAuthSessionStore(IConnectionMultiplexer redis, SessionKeyBuilder keys)
    {
        _redis = redis;
        _keys = keys;
    }

    /// <inheritdoc />
    public async Task StoreAsync(AuthSession session, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(session, JsonOptions);

        var txn = db.CreateTransaction();
        _ = txn.StringSetAsync(_keys.SessionKey(session.SessionId), json, ttl);
        _ = txn.SetAddAsync(_keys.UserSessionsKey(session.UserId), session.SessionId.ToString("N"));
        await txn.ExecuteAsync();
    }

    /// <inheritdoc />
    public async Task<AuthSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(_keys.SessionKey(sessionId));
        if (value.IsNullOrEmpty)
        {
            return null;
        }
        return JsonSerializer.Deserialize<AuthSession>(value.ToString(), JsonOptions);
    }

    /// <inheritdoc />
    public async Task TouchAsync(Guid sessionId, TimeSpan newTtl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = _keys.SessionKey(sessionId);
        var existing = await db.StringGetAsync(key);
        if (existing.IsNullOrEmpty)
        {
            return; // Session yok; touch atomic'liği için sessiz geç.
        }

        var session = JsonSerializer.Deserialize<AuthSession>(existing.ToString(), JsonOptions);
        if (session is null)
        {
            return;
        }
        session.LastActivity = DateTimeOffset.UtcNow;
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await db.StringSetAsync(key, json, newTtl);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AuthSession session, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await db.StringSetAsync(_keys.SessionKey(session.SessionId), json, ttl);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var txn = db.CreateTransaction();
        _ = txn.KeyDeleteAsync(_keys.SessionKey(sessionId));
        _ = txn.SetRemoveAsync(_keys.UserSessionsKey(userId), sessionId.ToString("N"));
        await txn.ExecuteAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var sessionIds = await db.SetMembersAsync(_keys.UserSessionsKey(userId));
        if (sessionIds.Length == 0)
        {
            return;
        }

        var keys = sessionIds
            .Select(sid => (RedisKey)_keys.SessionKey(Guid.ParseExact(sid!, "N")))
            .ToArray();

        var txn = db.CreateTransaction();
        _ = txn.KeyDeleteAsync(keys);
        _ = txn.KeyDeleteAsync(_keys.UserSessionsKey(userId));
        await txn.ExecuteAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetActiveSessionIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var values = await db.SetMembersAsync(_keys.UserSessionsKey(userId));
        return [.. values.Select(v => Guid.ParseExact(v!, "N"))];
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePreservingTtlAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = _keys.SessionKey(session.SessionId);

        // Mevcut kalan TTL'i oku — session yoksa / TTL'siz ise güncelleme yapma.
        var ttl = await db.KeyTimeToLiveAsync(key);
        if (ttl is null)
        {
            return false;
        }

        var json = JsonSerializer.Serialize(session, JsonOptions);
        await db.StringSetAsync(key, json, ttl);
        return true;
    }
}
