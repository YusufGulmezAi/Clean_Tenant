using System.Net;
using System.Net.Http.Json;
using CleanTenant.WebApi.IntegrationTests.Fixtures;

namespace CleanTenant.WebApi.IntegrationTests.Auth;

[Collection(nameof(WebApiCollection))]
public sealed class RefreshTokenTests
{
    private readonly WebApiFactoryFixture _fixture;

    public RefreshTokenTests(WebApiFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Gecerli_refresh_yeni_token_pair_donmeli()
    {
        var (_, _, refreshToken) = await _fixture.CreateAuthenticatedClientAsync();
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<RefreshResponse>();
        json.Should().NotBeNull();
        json!.AccessToken.Should().NotBeNullOrEmpty();
        json.RefreshToken.Should().NotBeNullOrEmpty().And.NotBe(refreshToken);
    }

    [Fact]
    public async Task Kullanilmis_refresh_replay_olarak_401_donmeli()
    {
        var (_, _, refreshToken) = await _fixture.CreateAuthenticatedClientAsync();
        var client = _fixture.CreateClient();

        // İlk refresh — başarılı
        var first = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Aynı (kullanılmış) refresh ile tekrar → replay
        var second = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bilinmeyen_refresh_401_donmeli()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "bogus-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bos_refresh_400_donmeli()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record RefreshResponse(
        string AccessToken,
        string RefreshToken,
        Guid SessionId);
}
