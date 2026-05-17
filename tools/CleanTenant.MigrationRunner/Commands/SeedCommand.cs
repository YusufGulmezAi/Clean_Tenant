using System.CommandLine;
using CleanTenant.Infrastructure.Persistence.Seeding;
using CleanTenant.MigrationRunner.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanTenant.MigrationRunner.Commands;

/// <summary>
/// <c>seed</c> alt komutu. Tüm ortamlar için ortak (Permission + built-in
/// roller) seed'i ve ortama özel ek seed (Dev/Demo) uygular.
/// </summary>
internal static class SeedCommand
{
    /// <summary>System.CommandLine için komut tanımını üretir.</summary>
    public static Command Build()
    {
        var envOption = new Option<string>("--env")
        {
            Description = "Hedef ortam (Development | Test | Demo | Production).",
            Required = true,
        };

        var command = new Command("seed", "Permission + built-in roller + ortama özel seed.")
        {
            envOption,
        };

        command.SetAction(async parseResult =>
        {
            var env = parseResult.GetValue(envOption)!;
            await ExecuteAsync(env);
            return 0;
        });

        return command;
    }

    private static async Task ExecuteAsync(string environment)
    {
        using var host = HostBuilderFactory.Build(environment);
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        using var scope = host.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<CatalogSeeder>();

        logger.LogInformation("[{Env}] Permission ve built-in rol seed başlatıldı.", environment);
        await seeder.SeedCoreCatalogAsync();

        // Ortama özel seed
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            var dev = scope.ServiceProvider.GetRequiredService<DevSeedData>();
            await dev.SeedAsync();
        }
        else if (string.Equals(environment, "Demo", StringComparison.OrdinalIgnoreCase))
        {
            var demo = scope.ServiceProvider.GetRequiredService<DemoSeedData>();
            await demo.SeedAsync();
        }
        else if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "[{Env}] Production ortamı: yalnız Permission + built-in roller seed'lendi. "
                + "Kullanıcı yaratmak için 'init-system-admin' komutunu çalıştırın.",
                environment);
        }
        else if (string.Equals(environment, "Test", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "[{Env}] Test ortamı: yalnız Permission + built-in roller seed'lendi.",
                environment);
        }

        logger.LogInformation("[{Env}] Seed tamamlandı.", environment);
    }
}
