using CleanTenant.Application.Common.Auditing;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Main.Accruals.Distribution;
using CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;
using CleanTenant.Application.Features.Main.Accruals.Posting;
using CleanTenant.Application.Features.Main.LateFees.Calculation;
using CleanTenant.Infrastructure.Persistence;
using CleanTenant.Infrastructure.Persistence.Audit;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.Persistence.Main;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CleanTenant.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// <para>
/// FAZ 8 — Bütçe/tahakkuk/tahsilat/gecikme uçtan-uca (E2E) test altyapısı.
/// <see cref="PostgresFixture"/>'a ek olarak <b>tam Application service grafiğini</b>
/// kurar: MediatR handler'ları + dağıtım/yevmiye/gecikme servisleri + kontrol
/// edilebilir <see cref="TestClock"/>/<see cref="TestSessionAccessor"/>. Pipeline
/// behavior'ları (auth/validation/caching/logging) bilinçli olarak kayıt edilmez —
/// E2E iş mantığını doğrular, yetki/oturum altyapısını değil.
/// </para>
/// </summary>
public sealed class BudgetE2EFixture : IAsyncLifetime
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

    /// <summary>Migrations uygulanmış, tam Application DI provider.</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>Tenant context mock — test tenant kimliğini set eder.</summary>
    public ITenantContext TenantContext { get; } = Substitute.For<ITenantContext>();

    /// <summary>Kontrol edilebilir saat.</summary>
    public TestClock Clock { get; } = new();

    /// <summary>Kontrol edilebilir oturum (varsayılan null = sistem).</summary>
    public TestSessionAccessor Session { get; } = new();

    private readonly IAuditMetadataAccessor _auditMetadata = Substitute.For<IAuditMetadataAccessor>();

    /// <summary>Container'ı başlatır, 3 DB + extension + migration kurar, DI'yı oluşturur.</summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var catalogConn = _container.GetConnectionString();
        var auditConn = catalogConn.Replace($"Database={CatalogDatabase}", $"Database={AuditDatabase}", StringComparison.Ordinal);
        var mainConn = catalogConn.Replace($"Database={CatalogDatabase}", $"Database={MainDatabase}", StringComparison.Ordinal);

        await CreateDatabaseAsync(catalogConn, AuditDatabase);
        await CreateDatabaseAsync(catalogConn, MainDatabase);
        await CreateExtensionsAsync(catalogConn);
        await CreateExtensionsAsync(auditConn);
        await CreateExtensionsAsync(mainConn);

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddDataProtection();

        _auditMetadata.Capture().Returns(new AuditMetadata
        {
            EnvironmentName = "Test",
            MachineName = "test-host",
            ApplicationName = "CleanTenant.Test",
            ApplicationVersion = "0.0.0-test",
            ProcessId = Environment.ProcessId,
            ThreadId = Environment.CurrentManagedThreadId,
        });
        services.AddSingleton(_auditMetadata);

        // Persistence (Catalog + Audit + Main). Main; IAccountCodeAllocator'ı da kayıt eder.
        services.AddCatalogPersistence(catalogConn, auditConn);
        services.AddAuditPersistence(auditConn);
        services.AddMainPersistence(mainConn, auditConn);

        // Test çift'leri — tenant filtresi + saat + oturum.
        services.AddScoped(_ => TenantContext);
        services.AddSingleton<IClock>(Clock);
        services.AddScoped<ICurrentSessionAccessor>(_ => Session);

        // MediatR handler'ları (pipeline behavior YOK) + çekirdek servisler.
        services.AddMediatR(typeof(GenerateBudgetAccrualCommand).Assembly);
        services.AddSingleton<IDistributionService, DistributionService>();
        services.AddScoped<IAccrualJournalPoster, AccrualJournalPoster>();
        services.AddSingleton<ILateFeeCalculator, LateFeeCalculator>();
        services.AddSingleton<ILateFeePolicyResolver, LateFeePolicyResolver>();

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<MainDbContext>().Database.MigrateAsync();
    }

    /// <summary>Container'ı kapatır.</summary>
    public async Task DisposeAsync()
    {
        if (Services is IDisposable d) d.Dispose();
        await _container.DisposeAsync();
    }

    /// <summary>Test tenant kimliğini global query filter için ayarlar.</summary>
    public void SetTenant(Guid tenantId)
    {
        TenantContext.TenantId.Returns(tenantId);
        TenantContext.CompanyId.Returns((Guid?)null);
        TenantContext.UnitId.Returns((Guid?)null);
        TenantContext.CurrentScope.Returns(ScopeLevel.Tenant);
    }

    private static async Task CreateDatabaseAsync(string adminConn, string dbName)
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
        try { await cmd.ExecuteNonQueryAsync(); }
        catch (PostgresException ex) when (ex.SqlState == "42P04") { /* already exists */ }
    }

    private static async Task CreateExtensionsAsync(string conn)
    {
        await using var c = new NpgsqlConnection(conn);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            CREATE EXTENSION IF NOT EXISTS citext;
            CREATE EXTENSION IF NOT EXISTS unaccent;
            CREATE EXTENSION IF NOT EXISTS pg_trgm;
            CREATE EXTENSION IF NOT EXISTS pgcrypto;
        """;
        await cmd.ExecuteNonQueryAsync();
    }
}
