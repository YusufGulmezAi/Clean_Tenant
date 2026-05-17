using CleanTenant.Application.Common.Auditing;
using CleanTenant.Infrastructure.Persistence;
using CleanTenant.SharedKernel.Context;
using CleanTenant.Infrastructure.Persistence.Audit;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.Persistence.Main;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CleanTenant.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// <para>
/// Test sınıfı seviyesinde paylaşılan PostgreSQL container'ı. v0.2.3.a'dan
/// itibaren <b>üç database</b> üretir: <c>cleantenant_catalog</c> +
/// <c>cleantenant_audit</c> + <c>cleantenant_main</c>. MainDbContext testleri
/// üçüncüsünü kullanır.
/// </para>
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string CatalogDatabase = "cleantenant_catalog";
    private const string AuditDatabase = "cleantenant_audit";
    private const string MainDatabase = "cleantenant_main";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase(CatalogDatabase)
        .WithUsername("cleantenant")
        .WithPassword("test-only-password")
        .Build();

    /// <summary>Catalog DB bağlantı dizgesi.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Audit DB bağlantı dizgesi (v0.1.7).</summary>
    public string AuditConnectionString { get; private set; } = string.Empty;

    /// <summary>Main DB bağlantı dizgesi (v0.2.3.a).</summary>
    public string MainConnectionString { get; private set; } = string.Empty;

    /// <summary>Migrations uygulanmış DI provider. (Catalog + Main + Audit aynı DI'da.)</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>IAuditMetadataAccessor mock'u — testler set ederek metadata enjekte eder.</summary>
    public IAuditMetadataAccessor AuditMetadataAccessor { get; } =
        Substitute.For<IAuditMetadataAccessor>();

    /// <summary>ITenantContext mock'u — testler Main DB global query filter için kullanır.</summary>
    public ITenantContext TenantContext { get; } = Substitute.For<ITenantContext>();

    /// <summary>Container'ı başlatır, üç DB oluşturur, extension'ları yükler, migration'ları uygular.</summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        AuditConnectionString = ConnectionString.Replace(
            $"Database={CatalogDatabase}", $"Database={AuditDatabase}", StringComparison.Ordinal);
        MainConnectionString = ConnectionString.Replace(
            $"Database={CatalogDatabase}", $"Database={MainDatabase}", StringComparison.Ordinal);

        await CreateExtraDatabaseAsync(ConnectionString, AuditDatabase);
        await CreateExtraDatabaseAsync(ConnectionString, MainDatabase);
        await CreatePostgresExtensionsAsync(ConnectionString);
        await CreatePostgresExtensionsAsync(AuditConnectionString);
        await CreatePostgresExtensionsAsync(MainConnectionString);

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddDataProtection();

        AuditMetadataAccessor.Capture().Returns(new AuditMetadata
        {
            EnvironmentName = "Test",
            MachineName = "test-host",
            ApplicationName = "CleanTenant.Test",
            ApplicationVersion = "0.0.0-test",
            ProcessId = Environment.ProcessId,
            ThreadId = Environment.CurrentManagedThreadId,
        });
        services.AddSingleton(AuditMetadataAccessor);

        services.AddCatalogPersistence(ConnectionString, AuditConnectionString);
        services.AddAuditPersistence(AuditConnectionString);
        services.AddMainPersistence(MainConnectionString, AuditConnectionString);

        // AddCatalogPersistence içindeki ITenantContext placeholder kayıtını override et —
        // testler Main DB global query filter senaryoları için tenant kimliğini set edebilir.
        services.AddScoped(_ => TenantContext);

        Services = services.BuildServiceProvider();

        await ApplyMigrationsAsync(Services);
    }

    /// <summary>Container'ı durdurur ve siler.</summary>
    public async Task DisposeAsync()
    {
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
        await _container.DisposeAsync();
    }

    /// <summary>Ek database'i postgres'in initial DB'sinde manuel CREATE eder.</summary>
    private static async Task CreateExtraDatabaseAsync(string catalogConnectionString, string databaseName)
    {
        await using var conn = new NpgsqlConnection(catalogConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42P04")
        {
            // 42P04: database already exists — sessiz geç.
        }
    }

    private static async Task CreatePostgresExtensionsAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE EXTENSION IF NOT EXISTS citext;
            CREATE EXTENSION IF NOT EXISTS unaccent;
            CREATE EXTENSION IF NOT EXISTS pg_trgm;
            CREATE EXTENSION IF NOT EXISTS pgcrypto;
        """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ApplyMigrationsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await catalog.Database.MigrateAsync();
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await audit.Database.MigrateAsync();
        var main = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        await main.Database.MigrateAsync();
    }
}
