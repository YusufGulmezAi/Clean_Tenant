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
/// <para>
/// v0.1.5.c'den itibaren test admin <b>2FA enrolled</b> olarak seed edilir
/// (System kullanıcılarda 2FA zorunlu). Login akışı challenge dönerse fixture
/// UserManager üzerinden gerçek TOTP kodu üretip <c>/2fa/verify</c> akışını
/// yürütür — testler bunu görmeden Bearer'lı client alır.
/// </para>
/// </summary>
public sealed class WebApiFactoryFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Test admin kullanıcı e-posta.</summary>
    public const string TestAdminEmail = "test.admin@cleantenant.test";

    /// <summary>Test admin kullanıcı şifresi (policy uyumlu).</summary>
    public const string TestAdminPassword = "TestPass-2026!";

    private const string AuthenticatorProvider = "Authenticator";
    private const string EmailProvider = "Email";

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

    /// <summary>
    /// Login + 2FA verify akışını yürütüp Bearer header'la authenticate edilmiş
    /// HttpClient döner. Test admin System scope'unda olduğu için 2FA zorunlu;
    /// fixture TOTP kodu üretip otomatik verify eder.
    /// </summary>
    public async Task<(HttpClient Client, string AccessToken, string RefreshToken)> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var loginBody = new
        {
            identifier = TestAdminEmail,
            password = TestAdminPassword,
            persona = "Management",
            contextId = (Guid?)null,
        };

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginBody);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseShape>();
        loginResult.Should().NotBeNull();

        TokenResponse tokens;
        if (string.Equals(loginResult!.Status, "Success", StringComparison.OrdinalIgnoreCase))
        {
            tokens = loginResult.Tokens
                ?? throw new InvalidOperationException("Login başarılı ama tokens null.");
        }
        else
        {
            // 2FA challenge — fixture TOTP üretip verify eder
            var challenge = loginResult.Challenge
                ?? throw new InvalidOperationException("Challenge yok.");

            // AuthenticatorTokenProvider sunucuda TOTP üretemez (secret yalnız
            // kullanıcının authenticator app'inde). Test fixture'ı için Email
            // provider'ı kullanıyoruz — TotpSecurityStampBasedTokenProvider'dan
            // türediği için sunucu kodu üretebilir.
            string code;
            using (var scope = Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var admin = await userManager.FindByEmailAsync(TestAdminEmail)
                    ?? throw new InvalidOperationException("Test admin bulunamadı.");
                code = await userManager.GenerateTwoFactorTokenAsync(admin, EmailProvider);
            }

            var verifyBody = new
            {
                challengeToken = challenge.ChallengeToken,
                method = EmailProvider,
                code,
            };
            var verifyResponse = await client.PostAsJsonAsync("/api/v1/auth/2fa/verify", verifyBody);
            verifyResponse.EnsureSuccessStatusCode();
            tokens = (await verifyResponse.Content.ReadFromJsonAsync<TokenResponse>())!;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
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

        // Test admin kullanıcı (System scope) — 2FA enrolled
        var user = new User
        {
            UserName = TestAdminEmail,
            Email = TestAdminEmail,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "Admin",
        };
        var result = await userManager.CreateAsync(user, TestAdminPassword);
        result.Succeeded.Should().BeTrue();

        // System kullanıcısı için 2FA zorunlu — TOTP enrollment'ı seed sırasında yap
        await userManager.ResetAuthenticatorKeyAsync(user);
        await userManager.SetTwoFactorEnabledAsync(user, true);

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

    /// <summary>Login response polimorfik yapısı (Status discriminator).</summary>
    private sealed record LoginResponseShape(
        string Status,
        TokenResponse? Tokens,
        ChallengeResponseShape? Challenge);

    /// <summary>2FA challenge response yapısı.</summary>
    private sealed record ChallengeResponseShape(
        Guid ChallengeToken,
        DateTimeOffset ExpiresAt,
        IReadOnlyList<string> AvailableMethods);

    /// <summary>Login / verify başarı response yapısı.</summary>
    private sealed record TokenResponse(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        Guid SessionId,
        Guid ContextId);
}
