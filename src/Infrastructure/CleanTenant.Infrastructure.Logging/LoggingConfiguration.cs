using System.Globalization;
using CleanTenant.Infrastructure.Logging.Enrichers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NpgsqlTypes;
using Serilog;
using Serilog.Sinks.PostgreSQL;

namespace CleanTenant.Infrastructure.Logging;

/// <summary>
/// Serilog wiring extension'ı. WebApi composition root'tan tek satırla çağrılır.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// <para>
    /// CleanTenant için Serilog yapılandırması:
    /// </para>
    /// <list type="bullet">
    ///   <item>Console sink (her zaman aktif).</item>
    ///   <item>PostgreSQL sink (<c>logs</c> tablosu) — <c>ConnectionStrings:Log</c> varsa.</item>
    ///   <item>Enricher'lar: machine/process/thread + custom <see cref="AuditMetadataEnricher"/>.</item>
    ///   <item>Minimum level: <c>Serilog:MinimumLevel</c> config; default Development=Debug, diğerleri=Information.</item>
    /// </list>
    /// </summary>
    public static WebApplicationBuilder AddCleanTenantSerilog(this WebApplicationBuilder builder)
    {
        // AuditMetadataEnricher singleton — HTTP scope'undan IAuditMetadataAccessor resolve eder
        builder.Services.AddSingleton<AuditMetadataEnricher>();

        builder.Host.UseSerilog((context, services, loggerConfig) =>
        {
            var enricher = services.GetRequiredService<AuditMetadataEnricher>();
            var defaultLevel = context.HostingEnvironment.IsDevelopment()
                ? Serilog.Events.LogEventLevel.Debug
                : Serilog.Events.LogEventLevel.Information;

            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .MinimumLevel.Is(defaultLevel)
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.With(enricher)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture);

            var logConnectionString = context.Configuration.GetConnectionString("Log");
            if (!string.IsNullOrWhiteSpace(logConnectionString))
            {
                loggerConfig.WriteTo.PostgreSQL(
                    connectionString: logConnectionString,
                    tableName: "logs",
                    columnOptions: BuildColumnOptions(),
                    needAutoCreateTable: false,
                    respectCase: false,
                    formatProvider: CultureInfo.InvariantCulture);
            }
        });

        return builder;
    }

    /// <summary>
    /// <c>logs</c> tablosunun kolon-eşlemesi. EF migration'la oluşan kolon adlarıyla bire bir hizalı.
    /// </summary>
    private static Dictionary<string, ColumnWriterBase> BuildColumnOptions() => new()
    {
        ["timestamp"] = new TimestampColumnWriter(NpgsqlDbType.TimestampTz),
        ["level"] = new LevelColumnWriter(renderAsText: false, dbType: NpgsqlDbType.Smallint),
        ["message"] = new RenderedMessageColumnWriter(NpgsqlDbType.Text),
        ["message_template"] = new MessageTemplateColumnWriter(NpgsqlDbType.Text),
        ["exception"] = new ExceptionColumnWriter(NpgsqlDbType.Text),
        ["properties"] = new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb),
        ["trace_id"] = new SinglePropertyColumnWriter("TraceId", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
        ["correlation_id"] = new SinglePropertyColumnWriter("CorrelationId", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
    };
}
