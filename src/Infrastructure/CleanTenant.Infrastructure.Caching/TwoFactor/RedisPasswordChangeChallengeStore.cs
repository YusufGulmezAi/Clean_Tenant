using System.Text.Json;
using CleanTenant.Application.Common.Auth;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.TwoFactor;

/// <summary>
/// <see cref="IPasswordChangeChallengeStore"/>'un StackExchange.Redis implementasyonu.
/// Challenge JSON olarak <c>{prefix}:pwd-chg:{token}</c> anahtarında
/// 15 dk default TTL ile saklanır.
/// </summary>
public sealed class RedisPasswordChangeChallengeStore : IPasswordChangeChallengeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly string _prefix;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RedisPasswordChangeChallengeStore(
        IConnectionMultiplexer redis,
        IOptions<SessionSettings> sessionOptions)
    {
        _redis = redis;
        _prefix = sessionOptions.Value.KeyPrefix;
    }

    private string Key(Guid token) => $"{_prefix}:pwd-chg:{token:N}";

    /// <inheritdoc />
    public async Task StoreAsync(PasswordChangeChallenge challenge, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(challenge, JsonOptions);
        await db.StringSetAsync(Key(challenge.ChallengeToken), json, ttl);
    }

    /// <inheritdoc />
    public async Task<PasswordChangeChallenge?> GetAsync(Guid challengeToken, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(Key(challengeToken));
        if (value.IsNullOrEmpty)
        {
            return null;
        }
        return JsonSerializer.Deserialize<PasswordChangeChallenge>(value.ToString(), JsonOptions);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid challengeToken, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(Key(challengeToken));
    }
}
