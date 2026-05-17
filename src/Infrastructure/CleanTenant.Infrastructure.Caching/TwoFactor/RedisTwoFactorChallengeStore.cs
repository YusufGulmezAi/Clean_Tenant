using System.Text.Json;
using CleanTenant.Application.Common.Auth;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.TwoFactor;

/// <summary>
/// <see cref="ITwoFactorChallengeStore"/>'un StackExchange.Redis ile gerçekleştirilmiş
/// implementasyonu. JSON serialize edilmiş challenge'ı <c>{prefix}:2fa:challenge:{token}</c>
/// anahtarında 5 dk default TTL ile tutar.
/// </summary>
public sealed class RedisTwoFactorChallengeStore : ITwoFactorChallengeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly string _prefix;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RedisTwoFactorChallengeStore(
        IConnectionMultiplexer redis,
        IOptions<SessionSettings> sessionOptions)
    {
        _redis = redis;
        _prefix = sessionOptions.Value.KeyPrefix;
    }

    private string Key(Guid token) => $"{_prefix}:2fa:challenge:{token:N}";

    /// <inheritdoc />
    public async Task StoreAsync(TwoFactorChallenge challenge, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(challenge, JsonOptions);
        await db.StringSetAsync(Key(challenge.ChallengeToken), json, ttl);
    }

    /// <inheritdoc />
    public async Task<TwoFactorChallenge?> GetAsync(Guid challengeToken, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(Key(challengeToken));
        if (value.IsNullOrEmpty)
        {
            return null;
        }
        return JsonSerializer.Deserialize<TwoFactorChallenge>(value.ToString(), JsonOptions);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid challengeToken, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(Key(challengeToken));
    }
}
