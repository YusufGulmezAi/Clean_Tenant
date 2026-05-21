using CleanTenant.Application.Common.Auth;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.Sessions;

/// <summary>
/// <see cref="IVerificationCodeService"/>'in StackExchange.Redis implementasyonu.
/// Kodlar <c>{prefix}:otp:{key}</c> anahtarında düz string olarak TTL ile saklanır.
/// </summary>
public sealed class RedisVerificationCodeService : IVerificationCodeService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _prefix;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RedisVerificationCodeService(
        IConnectionMultiplexer redis,
        IOptions<SessionSettings> sessionOptions)
    {
        _redis = redis;
        _prefix = sessionOptions.Value.KeyPrefix;
    }

    private string Key(string key) => $"{_prefix}:otp:{key}";

    /// <inheritdoc />
    public async Task<string> GenerateAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var code = Random.Shared.Next(100_000, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        var db = _redis.GetDatabase();
        await db.StringSetAsync(Key(key), code, ttl);
        return code;
    }

    /// <inheritdoc />
    public async Task<bool> VerifyAsync(string key, string code, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var redisKey = Key(key);
        var stored = await db.StringGetAsync(redisKey);
        if (stored.IsNullOrEmpty)
            return false;

        if (!string.Equals(stored.ToString(), code.Trim(), StringComparison.Ordinal))
            return false;

        // Tek kullanımlık — doğrulandıktan sonra sil
        await db.KeyDeleteAsync(redisKey);
        return true;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(Key(key));
    }
}
