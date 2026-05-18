using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.Application.Features.Main.Readers;
using CleanTenant.Infrastructure.Caching.Cache;
using CleanTenant.Infrastructure.Caching.Readers;
using CleanTenant.Infrastructure.Caching.Sessions;
using CleanTenant.Infrastructure.Caching.TwoFactor;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching;

/// <summary>
/// Caching katmanının DI kayıtları. Redis connection multiplexer'ı + session
/// store'u + v0.2.3.d hybrid cache mimarisi (HybridCacheStore + pub/sub +
/// reader pattern + invalidator).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Redis bağlantısını, session store'unu, key builder'ı ve v0.2.3.d hybrid
    /// cache altyapısını kayıt eder.
    /// </summary>
    /// <param name="services">DI servis koleksiyonu.</param>
    /// <param name="redisConnectionString">StackExchange.Redis bağlantı dizgesi.</param>
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        string redisConnectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<SessionKeyBuilder>();

        // v0.2.3.e — AuthSession L1 cache:
        // RedisAuthSessionStore stateless (ConnectionMultiplexer + SessionKeyBuilder)
        // → singleton güvenli. CachedAuthSessionStore decorator de singleton; instance
        // id pub/sub origin filtresi için stabil olmalı.
        services.AddSingleton<RedisAuthSessionStore>();
        services.AddSingleton<CachedAuthSessionStore>(sp => new CachedAuthSessionStore(
            sp.GetRequiredService<RedisAuthSessionStore>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<IConnectionMultiplexer>(),
            sp.GetRequiredService<ILogger<CachedAuthSessionStore>>(),
            instanceId: Guid.NewGuid().ToString("N")));
        services.AddSingleton<IAuthSessionStore>(sp => sp.GetRequiredService<CachedAuthSessionStore>());

        // Pub/sub subscriber — multi-instance L1 senkronizasyonu
        services.AddHostedService<AuthSessionInvalidationSubscriber>();

        // v0.1.5.c — 2FA login challenge store'u (5 dk TTL).
        services.AddScoped<ITwoFactorChallengeStore, RedisTwoFactorChallengeStore>();

        // v0.2.2.a — System scope kullanıcıları için pre-auth 2FA enrollment store (10 dk TTL).
        services.AddScoped<IPreAuthEnrollmentStore, RedisPreAuthEnrollmentStore>();

        // ─── v0.2.3.d — Generic hybrid cache mimarisi ───
        services.AddMemoryCache();

        // Singleton — instance id stabil olmalı (her boot'ta yeni). Origin filter
        // pub/sub'de kullanılır.
        services.AddSingleton(sp => new HybridCacheStore(
            sp.GetRequiredService<IConnectionMultiplexer>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<ILogger<HybridCacheStore>>(),
            instanceId: Guid.NewGuid().ToString("N")));
        services.AddSingleton<ICacheStore>(sp => sp.GetRequiredService<HybridCacheStore>());

        // Subscriber: pub/sub channel'a abone olur — diğer instance'ların
        // invalidation mesajları gelince local L1'i temizler.
        services.AddHostedService<CacheInvalidationSubscriber>();

        // Domain-specific reader'lar (cache + EF fallback)
        services.AddScoped<ITenantCatalogReader, TenantCatalogReader>();
        services.AddScoped<IMainCatalogReader, MainCatalogReader>();

        // CRUD handler'larının kullanacağı invalidator
        services.AddScoped<ICacheInvalidator, CacheInvalidator>();

        return services;
    }
}
