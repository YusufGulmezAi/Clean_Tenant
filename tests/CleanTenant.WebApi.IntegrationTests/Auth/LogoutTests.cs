using System.Net;
using System.Net.Http.Json;
using CleanTenant.WebApi.IntegrationTests.Fixtures;

namespace CleanTenant.WebApi.IntegrationTests.Auth;

[Collection(nameof(WebApiCollection))]
public sealed class LogoutTests
{
    private readonly WebApiFactoryFixture _fixture;

    public LogoutTests(WebApiFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_sonrasi_logout_200_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/v1/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_sonrasi_ayni_token_401_donmeli_revocation_calisiyor()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var first = await client.PostAsync("/api/v1/auth/logout", content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsync("/api/v1/auth/logout", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "Logout sonrası session Redis'ten silindi; aynı JWT 401 almalı.");
    }

    [Fact]
    public async Task Bearer_olmadan_logout_401_donmeli()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
