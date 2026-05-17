using CleanTenant.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanTenant.MigrationRunner.Infrastructure;

/// <summary>
/// CLI komutları için <see cref="IHost"/> oluşturan ortak factory.
/// Konfigürasyonu ortam değişkenlerinden okur ve Persistence katmanını DI'a kayıt eder.
/// </summary>
internal static class HostBuilderFactory
{
    /// <summary>
    /// Belirtilen ortam için bir IHost inşa eder.
    /// </summary>
    /// <param name="environment">ASPNETCORE_ENVIRONMENT karşılığı (Development, Test, Demo, Production).</param>
    /// <returns>DI servis koleksiyonu kurulmuş IHost.</returns>
    public static IHost Build(string environment)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddEnvironmentVariables();

        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(opts =>
        {
            opts.SingleLine = true;
            opts.TimestampFormat = "HH:mm:ss ";
        });

        var catalogConnection = builder.Configuration.GetConnectionString("Catalog")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Catalog bulunamadı. .env.<env> dosyası yüklenmiş mi?");

        // AspNet Identity AddDefaultTokenProviders zinciri IDataProtectionProvider ister
        // (DataProtectorTokenProvider için). WebApi/ManagementApp host'larında otomatik
        // gelir; MigrationRunner standalone olduğu için explicit eklenmeli — yoksa
        // seed UserManager<User>'i çözemez.
        builder.Services.AddDataProtection();
        builder.Services.AddCatalogPersistence(catalogConnection);

        return builder.Build();
    }
}
