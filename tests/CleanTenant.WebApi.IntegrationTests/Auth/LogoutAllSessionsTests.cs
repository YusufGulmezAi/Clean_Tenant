using System.Net;
using CleanTenant.WebApi.IntegrationTests.Fixtures;

namespace CleanTenant.WebApi.IntegrationTests.Auth;

[Collection(nameof(WebApiCollection))]
public sealed class LogoutAllSessionsTests
{
    private readonly WebApiFactoryFixture _fixture;

    public LogoutAllSessionsTests(WebApiFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Logout_all_sonrasi_token_401_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var logoutAll = await client.PostAsync("/api/v1/users/me/sessions/logout-all", content: null);
        logoutAll.StatusCode.Should().Be(HttpStatusCode.OK);

        // Aynı token ile bir sonraki istek 401 (session Redis'ten silindi)
        var afterLogout = await client.PostAsync("/api/v1/auth/logout", content: null);
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bearer_olmadan_logout_all_401_donmeli()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsync("/api/v1/users/me/sessions/logout-all", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
