using System.Text.Json.Serialization;
using CleanTenant.Infrastructure.Caching;
using CleanTenant.Infrastructure.Identity;
using CleanTenant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Json;

namespace CleanTenant.WebApi.Configuration;

/// <summary>
/// WebApi composition root için tek satır DI registration extension.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// CleanTenant WebApi'nin tüm bağımlılıklarını tek çağrıda kayıt eder:
    /// OpenAPI, Persistence (Catalog), Redis cache, Identity (JWT bearer + session).
    /// </summary>
    /// <param name="services">DI servis koleksiyonu.</param>
    /// <param name="configuration">Bağlantı string'leri ve JWT/Session ayarları için.</param>
    public static IServiceCollection AddCleanTenantApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var catalogConnection = configuration.GetConnectionString("Catalog")
            ?? throw new InvalidOperationException("ConnectionStrings:Catalog bulunamadı.");
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis bulunamadı.");

        services.AddOpenApi();
        services.AddCatalogPersistence(catalogConnection);
        services.AddRedisCache(redisConnection);
        services.AddIdentityServices(configuration);

        // Enum'lar JSON request/response'larda string olarak okunup yazılsın
        // (örn. "Management"/"Portal", "ReadOnly"/"WriteEnabled" gibi).
        services.Configure<JsonOptions>(opts =>
        {
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }
}
