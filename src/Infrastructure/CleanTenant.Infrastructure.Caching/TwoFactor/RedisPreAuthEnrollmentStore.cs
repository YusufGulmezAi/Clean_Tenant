using System.Text.Json;
using CleanTenant.Application.Common.Auth;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.TwoFactor;

/// <summary>
/// <see cref="IPreAuthEnrollmentStore"/>'un StackExchange.Redis implementasyonu.
/// Challenge JSON olarak <c>{prefix}:2fa:preauth-enroll:{token}</c> anahtarında
/// 10 dk default TTL ile saklanır. UpdateAsync mevcut TTL'yi koruyarak yeniden yazar.
/// v0.2.2.a'da eklendi.
/// </summary>
public sealed class RedisPreAuthEnrollmentStore : IPreAuthEnrollmentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly string _prefix;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RedisPreAuthEnrollmentStore(
        IConnectionMultiplexer redis,
        IOptions<SessionSettings> sessionOptions)
    {
        _redis = redis;
        _prefix = sessionOptions.Value.KeyPrefix;
    }

    private string Key(Guid token) => $"{_prefix}:2fa:preauth-enroll:{token:N}";

    /// <inheritdoc />
    public async Task StoreAsync(PreAuthEnrollmentChallenge challenge, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(challenge, JsonOptions);
        await db.StringSetAsync(Key(challenge.ChallengeToken), json, ttl);
    }

    /// <inheritdoc />
    public async Task<PreAuthEnrollmentChallenge?> GetAsync(Guid challengeToken, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(Key(challengeToken));
        if (value.IsNullOrEmpty)
        {
            return null;
        }
        return JsonSerializer.Deserialize<PreAuthEnrollmentChallenge>(value.ToString(), JsonOptions);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PreAuthEnrollmentChallenge challenge, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = Key(challenge.ChallengeToken);
        var remainingTtl = await db.KeyTimeToLiveAsync(key);
        if (remainingTtl is null)
        {
            // Key süresi dolmuş veya yok — sessiz geç, sonraki GetAsync null döner.
            return;
        }
        var json = JsonSerializer.Serialize(challenge, JsonOptions);
        await db.StringSetAsync(key, json, remainingTtl);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid challengeToken, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(Key(challengeToken));
    }
}
