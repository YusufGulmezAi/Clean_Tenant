using CleanTenant.Infrastructure.Persistence;
using CleanTenant.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CleanTenant.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// <para>
/// Test sınıfı seviyesinde paylaşılan PostgreSQL container'ı sağlar.
/// Container açılışı, gerekli extension'ların yüklenmesi ve EF Core
/// migration'larının uygulanmasını üstlenir.
/// </para>
/// <para>
/// <b>Kullanım:</b> Test sınıfı <c>IClassFixture&lt;PostgresFixture&gt;</c>
/// implement eder; ctor üzerinden fixture inject edilir. Her test scope açar,
/// işini yapar, scope dispose olur (DbContext kapanır). Tests arasında DB
/// state'i akıyorsa testler benzersiz UrlCode/Email gibi alanlarla çakışmadan
/// çalışacak şekilde yazılır.
/// </para>
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("cleantenant_catalog")
        .WithUsername("cleantenant")
        .WithPassword("test-only-password")
        .Build();

    /// <summary>Container'ın PostgreSQL bağlantı dizgesi (container start sonrası geçerli).</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Migrations uygulanmış ve servislerin kayıt olduğu DI provider. Her test
    /// <see cref="IServiceProvider.CreateScope"/> ile scoped servislere erişir.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>Container'ı başlatır, extension'ları yükler, migration'ları uygular.</summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await CreatePostgresExtensionsAsync(ConnectionString);

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddCatalogPersistence(ConnectionString);
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

    /// <summary>
    /// Container'da PostgreSQL extension'larını (citext / unaccent / pg_trgm / pgcrypto)
    /// yükler. EF migration'ı bu extension'lara dayalı kolon tipleri kullanır,
    /// o yüzden migration öncesi uygulanmalı.
    /// </summary>
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
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync();
    }
}
