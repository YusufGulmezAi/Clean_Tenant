using System.Net;
using System.Net.Http.Json;
using CleanTenant.WebApi.IntegrationTests.Fixtures;

namespace CleanTenant.WebApi.IntegrationTests.TwoFactor;

/// <summary>
/// v0.2.13 — Profil 2FA self-servis endpoint'leri: ana açma/kapama (System
/// scope kilidi), e-posta kod gönderme, telefon/yöntem doğrulama validasyonları.
/// Test admin System scope + 2FA aktif + Email onaylı + Authenticator kurulu.
/// </summary>
[Collection(nameof(WebApiCollection))]
public sealed class TwoFactorMethodEndpointsTests
{
    private readonly WebApiFactoryFixture _fixture;

    public TwoFactorMethodEndpointsTests(WebApiFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task System_scope_kullanici_2FA_yi_kapatamamali_403()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/2fa/enable", new { enabled = false });

        // Guard SetTwoFactorEnabledAsync'ten ÖNCE çalışır → kalıcı durum değişmez.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Email_kod_gonderme_200_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/v1/auth/2fa/email/send-code", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Phone_kod_gonderme_gecersiz_numara_400_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/2fa/phone/send-code", new { phone = "123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Yontem_kaldirma_gecersiz_yontem_400_donmeli()
    {
        var (client, _, _) = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/2fa/method/remove", new { method = "Foo" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Bearer_olmadan_enable_401_donmeli()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/2fa/enable", new { enabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
