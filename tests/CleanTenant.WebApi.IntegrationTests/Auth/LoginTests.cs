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
    public async Task Gecerli_credentials_2FA_challenge_donmeli()
    {
        // v0.1.5.c: TestAdmin System scope'unda + 2FA enrolled. Login direkt
        // TokenPair değil, challenge döner. Status="TwoFactorRequired".
        var client = _fixture.CreateClient();
        var body = new { identifier = WebApiFactoryFixture.TestAdminEmail, password = WebApiFactoryFixture.TestAdminPassword, persona = "Management", contextId = (Guid?)null };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<LoginShape>();
        json.Should().NotBeNull();
        json!.Status.Should().Be("TwoFactorRequired");
        json.Tokens.Should().BeNull();
        json.Challenge.Should().NotBeNull();
        json.Challenge!.ChallengeToken.Should().NotBe(Guid.Empty);
        json.Challenge.AvailableMethods.Should().Contain("Authenticator");
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

    private sealed record LoginShape(
        string Status,
        TokenShape? Tokens,
        ChallengeShape? Challenge);

    private sealed record TokenShape(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        Guid SessionId,
        Guid ContextId);

    private sealed record ChallengeShape(
        Guid ChallengeToken,
        DateTimeOffset ExpiresAt,
        IReadOnlyList<string> AvailableMethods);
}
