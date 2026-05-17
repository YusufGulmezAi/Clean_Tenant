using System.CommandLine;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.MigrationRunner.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanTenant.MigrationRunner.Commands;

/// <summary>
/// <c>migrate</c> alt komutu. Catalog DbContext'in bekleyen migration'larını
/// hedef ortamın DB'sine uygular.
/// </summary>
internal static class MigrateCommand
{
    /// <summary>System.CommandLine için komut tanımını üretir.</summary>
    public static Command Build()
    {
        var envOption = new Option<string>("--env")
        {
            Description = "Hedef ortam (Development | Test | Demo | Production).",
            Required = true,
        };

        var command = new Command("migrate", "Catalog DB migration'larını uygular.")
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

        logger.LogInformation("[{Env}] Catalog migration uygulanıyor...", environment);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync();

        logger.LogInformation("[{Env}] Migration tamamlandı.", environment);
    }
}
