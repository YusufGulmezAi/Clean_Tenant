using System.Net;
using System.Net.Http.Json;
using CleanTenant.WebApi.IntegrationTests.Fixtures;

namespace CleanTenant.WebApi.IntegrationTests.Auth;

[Collection(nameof(WebApiCollection))]
public sealed class SwitchContextTests
{
    private readonly WebApiFactoryFixture _fixture;

    public SwitchContextTests(WebApiFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Persona_uyumsuz_scope_a_gecis_403_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        // TestAdmin Management persona ile login; Unit scope persona uyumsuz → 403
        var body = new
        {
            scopeLevel = "Unit",
            tenantId = Guid.NewGuid(),
            companyId = Guid.NewGuid(),
            unitId = Guid.NewGuid(),
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/switch-context", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Atamasi_olmayan_tenant_scope_a_gecis_403_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        // TestAdmin sadece System scope'a atanmış; rastgele bir tenant'a geçemez.
        var body = new
        {
            scopeLevel = "Tenant",
            tenantId = Guid.NewGuid(),
            companyId = (Guid?)null,
            unitId = (Guid?)null,
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/switch-context", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Bearer_olmadan_switch_context_401_donmeli()
    {
        var client = _fixture.CreateClient();
        var body = new { scopeLevel = "System", tenantId = (Guid?)null, companyId = (Guid?)null, unitId = (Guid?)null };

        var response = await client.PostAsJsonAsync("/api/v1/auth/switch-context", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
