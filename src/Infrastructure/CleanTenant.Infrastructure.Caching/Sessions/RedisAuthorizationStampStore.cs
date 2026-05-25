using CleanTenant.Application.Common.Auth;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching.Sessions;

/// <summary>
/// <see cref="IAuthorizationStampStore"/>'un Redis implementasyonu. Damga, TTL'siz
/// (kalıcı) tek bir string anahtarda tutulur; doğrudan Redis'ten okunur (L1 yok) —
/// böylece bir yetki değişimi <b>bir sonraki istekte</b> kesin görünür.
/// </summary>
public sealed class RedisAuthorizationStampStore : IAuthorizationStampStore
{
    private const string Key = "cleantenant:v1:authz:stamp";
    private const string Default = "0";

    private readonly IConnectionMultiplexer _redis;

    /// <summary>DI bağımlılığını alır.</summary>
    public RedisAuthorizationStampStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc />
    public async Task<string> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var value = await _redis.GetDatabase().StringGetAsync(Key);
        return value.IsNullOrEmpty ? Default : value.ToString();
    }

    /// <inheritdoc />
    public Task BumpAsync(CancellationToken cancellationToken = default)
        => _redis.GetDatabase().StringSetAsync(Key, Guid.NewGuid().ToString("N"));
}
