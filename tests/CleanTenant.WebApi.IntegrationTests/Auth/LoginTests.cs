using System.Net;
using System.Net.Http.Json;
using CleanTenant.WebApi.IntegrationTests.Fixtures;

namespace CleanTenant.WebApi.IntegrationTests.Auth;

[Collection(nameof(WebApiCollection))]
public sealed class LoginTests
{
    private readonly WebApiFactoryFixture _fixture;

    public LoginTests(WebApiFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Gecerli_credentials_token_pair_donmeli()
    {
        var client = _fixture.CreateClient();
        var body = new { identifier = WebApiFactoryFixture.TestAdminEmail, password = WebApiFactoryFixture.TestAdminPassword, persona = "Management", contextId = (Guid?)null };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<LoginResponse>();
        json.Should().NotBeNull();
        json!.AccessToken.Should().NotBeNullOrEmpty();
        json.RefreshToken.Should().NotBeNullOrEmpty();
        json.SessionId.Should().NotBe(Guid.Empty);
        json.ContextId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Yanlis_sifre_401_donmeli()
    {
        var client = _fixture.CreateClient();
        var body = new { identifier = WebApiFactoryFixture.TestAdminEmail, password = "WrongPass-2026!", persona = "Management", contextId = (Guid?)null };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bilinmeyen_kullanici_401_donmeli()
    {
        var client = _fixture.CreateClient();
        var body = new { identifier = "nobody@nowhere.com", password = "Whatever-2026!", persona = "Management", contextId = (Guid?)null };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bos_identifier_400_donmeli()
    {
        var client = _fixture.CreateClient();
        var body = new { identifier = "", password = "Whatever-2026!", persona = "Management", contextId = (Guid?)null };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Taninmayan_identifier_400_donmeli()
    {
        var client = _fixture.CreateClient();
        // 11 haneli sayma ama checksum geçmez; @ yok; telefon değil
        var body = new { identifier = "abc-not-anything", password = "Whatever-2026!", persona = "Management", contextId = (Guid?)null };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record LoginResponse(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        Guid SessionId,
        Guid ContextId);
}
