using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CleanTenant.WebApi.IntegrationTests.Fixtures;

namespace CleanTenant.WebApi.IntegrationTests.Support;

/// <summary>
/// <para>
/// Support Mode akış testleri (v0.1.5.b.2). Senaryo:
/// </para>
/// <list type="number">
///   <item>System scope'taki operatör <c>enter</c> ile tenant'a girer → yeni JWT.</item>
///   <item><c>elevate</c> ReadOnly → WriteEnabled (JWT yenilenmez).</item>
///   <item><c>exit</c> ile operatör orijinal session'ına döner → yine yeni JWT.</item>
/// </list>
/// <para>
/// Negatif: yetkisiz çağrılar, Support Mode dışında <c>exit/elevate</c>, kısa Reason.
/// </para>
/// </summary>
[Collection(nameof(WebApiCollection))]
public sealed class SupportModeTests
{
    private readonly WebApiFactoryFixture _fixture;

    public SupportModeTests(WebApiFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Enter_elevate_exit_uctan_uca_basariyla_calismali()
    {
        var (client, originalAccess, _) = await _fixture.CreateAuthenticatedClientAsync();
        var tenantId = await _fixture.SeedTenantAsync("E2E Test Tenant");

        // Enter
        var enterBody = new { targetTenantId = tenantId, reason = "Müşteri talebi destek erişimi - TKT-42" };
        var enterResp = await client.PostAsJsonAsync("/api/v1/system/support/enter", enterBody);
        enterResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var enterTokens = await enterResp.Content.ReadFromJsonAsync<TokenResponse>();
        enterTokens!.AccessToken.Should().NotBeNullOrEmpty();
        enterTokens.AccessToken.Should().NotBe(originalAccess);

        // Support session JWT ile devam et
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", enterTokens.AccessToken);

        // Tenant Admin denetim endpoint'i — support session Tenant scope'unda olduğundan erişebilmeli
        var auditResp = await client.GetAsync("/api/v1/tenant/audit/support-access?page=0&pageSize=10");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Elevate (ReadOnly → WriteEnabled)
        var elevateBody = new { reason = "Yazma yetkisine ihtiyaç var - TKT-42 detay" };
        var elevateResp = await client.PostAsJsonAsync("/api/v1/system/support/elevate", elevateBody);
        elevateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Exit
        var exitResp = await client.PostAsync("/api/v1/system/support/exit", content: null);
        exitResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var exitTokens = await exitResp.Content.ReadFromJsonAsync<ExitResponse>();
        exitTokens!.AccessToken.Should().NotBeNullOrEmpty();
        exitTokens.AccessToken.Should().NotBe(enterTokens.AccessToken);
    }

    [Fact]
    public async Task Enter_kisa_reason_400_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();
        var tenantId = await _fixture.SeedTenantAsync("Validation Test Tenant");

        var body = new { targetTenantId = tenantId, reason = "kısa" };
        var response = await client.PostAsJsonAsync("/api/v1/system/support/enter", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Enter_bilinmeyen_tenant_404_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var body = new
        {
            targetTenantId = Guid.NewGuid(),
            reason = "Bulunmayan tenant için destek talebi - TKT-99",
        };
        var response = await client.PostAsJsonAsync("/api/v1/system/support/enter", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Bearer_olmadan_enter_401_donmeli()
    {
        var client = _fixture.CreateClient();
        var body = new
        {
            targetTenantId = Guid.NewGuid(),
            reason = "Yetkisiz çağrı testi - sebep gövdesi yeterince uzun.",
        };
        var response = await client.PostAsJsonAsync("/api/v1/system/support/enter", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Aktif_support_olmadan_exit_403_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        // Operatör System scope'unda, Support Mode'a hiç girmemiş.
        // SupportModeActive policy: IsSystemSession + SupportMode ∈ (ReadOnly/WriteEnabled/Full)
        // → koşul karşılanmadığı için 403.
        var response = await client.PostAsync("/api/v1/system/support/exit", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Aktif_support_olmadan_elevate_403_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var body = new { reason = "Yazma yetkisi gerek - sebep en az yirmi karakter." };
        var response = await client.PostAsJsonAsync("/api/v1/system/support/elevate", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSystemSupportSessions_listesi_okunabilmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/system/support-sessions?page=0&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record TokenResponse(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        Guid SessionId,
        Guid ContextId);

    private sealed record ExitResponse(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        Guid SessionId,
        Guid ContextId);
}
