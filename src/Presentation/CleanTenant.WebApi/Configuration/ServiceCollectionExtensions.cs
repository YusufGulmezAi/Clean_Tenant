using System.Text.Json.Serialization;
using CleanTenant.Application;
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
    /// OpenAPI, Persistence (Catalog), Redis cache, Identity (JWT bearer + session),
    /// notification sender'ları (Console default; Production'da gerçek sağlayıcı zorunlu).
    /// </summary>
    /// <param name="services">DI servis koleksiyonu.</param>
    /// <param name="configuration">Bağlantı string'leri ve JWT/Session ayarları için.</param>
    /// <param name="environment">Sender provider guard'ı için (Production'da Console reddedilir).</param>
    public static IServiceCollection AddCleanTenantApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var catalogConnection = configuration.GetConnectionString("Catalog")
            ?? throw new InvalidOperationException("ConnectionStrings:Catalog bulunamadı.");
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis bulunamadı.");
        // Audit ve Log DB bağlantıları opsiyonel — yoksa interceptor/sink kayıt edilmez.
        var auditConnection = configuration.GetConnectionString("Audit");
        var logConnection = configuration.GetConnectionString("Log");

        services.AddOpenApi();
        services.AddApplicationServices();
        services.AddCatalogPersistence(catalogConnection, auditConnection);
        services.AddRedisCache(redisConnection);
        services.AddIdentityServices(configuration);
        services.AddCleanTenantNotifications(configuration, environment);

        if (!string.IsNullOrWhiteSpace(auditConnection))
        {
            services.AddAuditPersistence(auditConnection);
        }
        if (!string.IsNullOrWhiteSpace(logConnection))
        {
            services.AddLogPersistence(logConnection);
        }

        // Enum'lar JSON request/response'larda string olarak okunup yazılsın
        // (örn. "Management"/"Portal", "ReadOnly"/"WriteEnabled" gibi).
        services.Configure<JsonOptions>(opts =>
        {
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }
}
