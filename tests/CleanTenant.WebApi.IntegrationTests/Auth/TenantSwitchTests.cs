using System.Net;
using System.Net.Http.Json;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.WebApi.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.WebApi.IntegrationTests.Auth;

/// <summary>
/// v0.2.3.b — AppBar "Aktif Tenant" dropdown akışı:
/// <list type="bullet">
///   <item>GET /api/v1/auth/accessible-tenants — kullanıcının görebileceği tenant'lar</item>
///   <item>POST /api/v1/auth/switch-tenant — cross-tenant context geçişi</item>
/// </list>
/// Fixture'ın test admin'i System scope'ta — tüm Active tenant'ları görmeli.
/// </summary>
[Collection(nameof(WebApiCollection))]
public sealed class TenantSwitchTests
{
    private readonly WebApiFactoryFixture _fixture;

    public TenantSwitchTests(WebApiFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AccessibleTenants_anonim_401_donmeli()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/accessible-tenants");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessibleTenants_System_scope_tum_Active_tenantlari_donmeli()
    {
        var tenantId1 = await _fixture.SeedTenantAsync($"AccessibleA-{Guid.NewGuid():N}");
        var tenantId2 = await _fixture.SeedTenantAsync($"AccessibleB-{Guid.NewGuid():N}");

        // Pasif tenant da yarat — listede görünmemeli
        var inactiveTenantId = await SeedInactiveTenantAsync($"Inactive-{Guid.NewGuid():N}");

        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/auth/accessible-tenants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tenants = await response.Content.ReadFromJsonAsync<List<TenantShape>>();
        tenants.Should().NotBeNull();
        tenants.Should().Contain(t => t.TenantId == tenantId1);
        tenants.Should().Contain(t => t.TenantId == tenantId2);
        tenants.Should().NotContain(t => t.TenantId == inactiveTenantId);
        tenants.Should().OnlyHaveUniqueItems(t => t.TenantId);
    }

    [Fact]
    public async Task SwitchTenant_anonim_401_donmeli()
    {
        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/switch-tenant",
            new { tenantId = Guid.NewGuid() });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SwitchTenant_olmayan_tenant_404_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/switch-tenant",
            new { tenantId = Guid.NewGuid() });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SwitchTenant_System_user_herhangi_bir_Active_tenanta_gecebilmeli()
    {
        var tenantId = await _fixture.SeedTenantAsync($"Switch-Target-{Guid.NewGuid():N}");

        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/switch-tenant",
            new { tenantId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<TokenShape>();
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrEmpty();
        tokens.CurrentScope.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task SwitchTenant_pasif_tenanta_gecis_404_donmeli()
    {
        var inactiveTenantId = await SeedInactiveTenantAsync($"PasifTarget-{Guid.NewGuid():N}");

        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/switch-tenant",
            new { tenantId = inactiveTenantId });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedInactiveTenantAsync(string name)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var tenant = new Tenant
        {
            Name = name,
            Status = TenantStatus.Suspended,
            BillingTier = BillingTier.Standard,
            HasDedicatedDatabase = false,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }

    private sealed record TenantShape(Guid TenantId, string UrlCode, string Name);

    private sealed record TokenShape(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        Guid SessionId,
        Guid ContextId,
        ScopeShape CurrentScope);

    private sealed record ScopeShape(string Level, Guid? TenantId, Guid? CompanyId, Guid? UnitId);
}
