using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire arka plan job altyapısının DI kaydı. Depolama ayrı <c>cleantenant_jobs</c>
/// DB'sinde <c>hangfire</c> schema'sında (kullanıcı kararı 2026-05-24). Job'lar
/// uygulama-geneli; tenant izolasyonu job içinde sentetik sistem oturumu ile sağlanır.
/// </summary>
public static class BackgroundJobsDependencyInjection
{
    /// <summary>Hangfire tablolarının yaşadığı PostgreSQL schema adı.</summary>
    public const string HangfireSchema = "hangfire";

    /// <summary>
    /// Hangfire + PostgreSQL storage + işleyici sunucusunu kaydeder. Storage şemasını
    /// gerekirse otomatik oluşturur (<c>PrepareSchemaIfNecessary</c>).
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services, string jobsConnectionString)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                c => c.UseNpgsqlConnection(jobsConnectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = HangfireSchema,
                    PrepareSchemaIfNecessary = true,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                }));

        services.AddHangfireServer(options =>
        {
            options.ServerName = $"cleantenant-{Environment.MachineName}";
        });

        return services;
    }
}
