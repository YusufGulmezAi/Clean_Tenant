using CleanTenant.Application.Common.Auth;
using CleanTenant.Infrastructure.Caching.Sessions;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CleanTenant.Infrastructure.Caching;

/// <summary>
/// Caching katmanının DI kayıtları. Redis connection multiplexer'ı singleton
/// olarak kayıt eder; session store'u scoped olarak.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Redis bağlantısını, session store'unu ve key builder'ı kayıt eder.
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
        services.AddScoped<IAuthSessionStore, RedisAuthSessionStore>();

        return services;
    }
}
