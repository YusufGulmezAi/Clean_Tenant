using System.Net.Http.Headers;
using System.Net.Http.Json;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.SharedKernel.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace CleanTenant.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// <para>
/// Integration test sınıflarının paylaştığı fixture. Testcontainers ile
/// PostgreSQL + Redis container'ları başlatır; WebApplicationFactory'i bu
/// container'lara yönlendirir; gerekli init (extension'lar, migration, seed
/// kullanıcı) yapar.
/// </para>
/// </summary>
public sealed class WebApiFactoryFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Test admin kullanıcı e-posta.</summary>
    public const string TestAdminEmail = "test.admin@cleantenant.test";

    /// <summary>Test admin kullanıcı şifresi (policy uyumlu).</summary>
    public const string TestAdminPassword = "TestPass-2026!";

    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("cleantenant_catalog")
        .WithUsername("cleantenant")
        .WithPassword("test-only-password")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:8-alpine")
        .Build();

    /// <summary>Test environment'ı ayarlar.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    /// <summary>
    /// Container'ları başlatır; bağlantı bilgilerini process env var olarak yazar
    /// (Program.cs WebApplicationBuilder bunlardan okur); migration + seed yapar.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Task.WhenAll(_pg.StartAsync(), _redis.StartAsync());
        await CreateExtensionsAsync(_pg.GetConnectionString());

        // Program.cs WebApplicationBuilder.Configuration'ı build sırasında
        // okur; o yüzden env var'ları Services'a erişmeden önce set ediyoruz.
        Environment.SetEnvironmentVariable("ConnectionStrings__Catalog", _pg.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", _redis.GetConnectionString());
        Environment.SetEnvironmentVariable("JWT_ISSUER", "cleantenant.test");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "cleantenant.test.clients");
        Environment.SetEnvironmentVariable("JWT_SIGNING_KEY", "test-signing-key-must-be-at-least-32-bytes-long!!");
        Environment.SetEnvironmentVariable("JWT_ACCESS_TOKEN_MINUTES", "15");
        Environment.SetEnvironmentVariable("JWT_REFRESH_TOKEN_DAYS", "7");
        Environment.SetEnvironmentVariable("SESSION_TTL_PADDING_MINUTES", "30");

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync();
        await SeedAsync(scope.ServiceProvider);
    }

    /// <summary>Container'ları durdurur.</summary>
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _pg.DisposeAsync();
        await _redis.DisposeAsync();
    }

    /// <summary>
    /// Test için yeni bir tenant kaydı oluşturur (Active, shared DB modunda).
    /// </summary>
    public async Task<Guid> SeedTenantAsync(string name)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var tenant = new Tenant
        {
            Name = name,
            Status = TenantStatus.Active,
            BillingTier = BillingTier.Standard,
            HasDedicatedDatabase = false,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }

    /// <summary>Bearer header'la önceden authenticate edilmiş HttpClient.</summary>
    public async Task<(HttpClient Client, string AccessToken, string RefreshToken)> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var loginBody = new { identifier = TestAdminEmail, password = TestAdminPassword, persona = "Management", contextId = (Guid?)null };
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginBody);
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return (client, tokens.AccessToken, tokens.RefreshToken);
    }

    private static async Task CreateExtensionsAsync(string connectionString)
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

    private static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<CatalogDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        // Developer rolünü oluştur
        var role = new Role
        {
            Name = "Developer",
            NormalizedName = "DEVELOPER",
            Scope = ScopeLevel.System,
            Description = "Test developer",
            IsBuiltIn = true,
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        // Test admin kullanıcı
        var user = new User
        {
            UserName = TestAdminEmail,
            Email = TestAdminEmail,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "Admin",
            TwoFactorEnabled = false,
        };
        var result = await userManager.CreateAsync(user, TestAdminPassword);
        result.Succeeded.Should().BeTrue();

        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = role.Id,
            ScopeLevel = ScopeLevel.System,
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Login response yapısı (test serializasyon için).</summary>
    private sealed record TokenResponse(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        Guid SessionId,
        Guid ContextId);
}
