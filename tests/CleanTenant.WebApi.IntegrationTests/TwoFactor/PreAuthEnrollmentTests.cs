using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.SharedKernel.Context;
using CleanTenant.WebApi.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.WebApi.IntegrationTests.TwoFactor;

/// <summary>
/// v0.2.2.a — Pre-auth 2FA enrollment akışı senaryoları. Test admin
/// (WebApiFactoryFixture) zaten 2FA enrolled olduğu için bu testler kendi
/// kullanıcısını seed eder: System scope rolünde + 2FA disabled.
/// Akış: login → status=EnrollmentRequired → start → complete → finalize.
/// </summary>
[Collection(nameof(WebApiCollection))]
public sealed class PreAuthEnrollmentTests
{
    private const string AuthenticatorProvider = "Authenticator";

    private readonly WebApiFactoryFixture _fixture;

    public PreAuthEnrollmentTests(WebApiFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_System_user_2FA_disabled_EnrollmentRequired_donmeli()
    {
        var (email, password) = await SeedSystemUserWithout2FaAsync("pre-auth-login");
        var client = _fixture.CreateClient();

        var loginBody = new
        {
            identifier = email,
            password,
            persona = "Management",
            contextId = (Guid?)null,
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginBody);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginShape>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("EnrollmentRequired");
        result.EnrollmentChallenge.Should().NotBeNull();
        result.EnrollmentChallenge!.Email.Should().Be(email);
        result.EnrollmentChallenge.ChallengeToken.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Start_secret_ve_otpauth_uri_donmeli()
    {
        var (client, challengeToken, email) = await BeginEnrollmentAsync("pre-auth-start");

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/2fa/enroll-pre-auth/start",
            new { challengeToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StartShape>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
        result.Secret.Should().NotBeNullOrEmpty();
        result.QrCodeUri.Should().StartWith("otpauth://totp/CleanTenant:");
        result.QrCodeUri.Should().Contain("secret=" + result.Secret);
    }

    [Fact]
    public async Task Bilinmeyen_challenge_token_start_da_401_donmeli()
    {
        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/2fa/enroll-pre-auth/start",
            new { challengeToken = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Yanlis_kod_complete_da_401_donmeli()
    {
        var (client, challengeToken, _) = await BeginEnrollmentAsync("pre-auth-wrong-code");

        // Start çağrısı authenticator key üretsin
        await client.PostAsJsonAsync("/api/v1/auth/2fa/enroll-pre-auth/start", new { challengeToken });

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/2fa/enroll-pre-auth/complete",
            new { challengeToken, code = "000000" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dogrulanmamis_challenge_finalize_403_donmeli()
    {
        var (client, challengeToken, _) = await BeginEnrollmentAsync("pre-auth-not-verified");

        // Start çağırıp Complete'i ATLA — challenge.VerifiedAt null kalır.
        await client.PostAsJsonAsync("/api/v1/auth/2fa/enroll-pre-auth/start", new { challengeToken });

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/2fa/enroll-pre-auth/finalize",
            new { challengeToken });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tam_akis_start_complete_finalize_token_donmeli()
    {
        var (client, challengeToken, email) = await BeginEnrollmentAsync("pre-auth-full-flow");

        // Start → authenticator key (Base32 secret) üretilir + döner
        var startResp = await client.PostAsJsonAsync(
            "/api/v1/auth/2fa/enroll-pre-auth/start",
            new { challengeToken });
        startResp.EnsureSuccessStatusCode();
        var startResult = await startResp.Content.ReadFromJsonAsync<StartShape>();
        startResult!.Secret.Should().NotBeNullOrEmpty();

        // RFC 6238 ile gerçek TOTP üret (ASP.NET Identity'nin
        // AuthenticatorTokenProvider.GenerateAsync'i boş döner — kod app'ten gelir.)
        var totpCode = GenerateTotp(startResult.Secret);

        // Complete → 2FA enable + recovery codes
        var completeResp = await client.PostAsJsonAsync(
            "/api/v1/auth/2fa/enroll-pre-auth/complete",
            new { challengeToken, code = totpCode });
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var completeResult = await completeResp.Content.ReadFromJsonAsync<CompleteShape>();
        completeResult.Should().NotBeNull();
        completeResult!.RecoveryCodes.Should().HaveCount(10);
        completeResult.RecoveryCodes.Should().OnlyHaveUniqueItems();

        // Finalize → TokenPair
        var finalizeResp = await client.PostAsJsonAsync(
            "/api/v1/auth/2fa/enroll-pre-auth/finalize",
            new { challengeToken });
        finalizeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await finalizeResp.Content.ReadFromJsonAsync<TokenShape>();
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
        tokens.SessionId.Should().NotBe(Guid.Empty);

        // Finalize sonrası challenge silinmeli — ikinci finalize 401 dönmeli
        var secondFinalize = await client.PostAsJsonAsync(
            "/api/v1/auth/2fa/enroll-pre-auth/finalize",
            new { challengeToken });
        secondFinalize.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Kullanıcının veritabanında 2FA aktif olduğunu doğrula
        using (var scope = _fixture.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);
            user.Should().NotBeNull();
            user!.TwoFactorEnabled.Should().BeTrue();
        }
    }

    /// <summary>Yeni bir System scope user yaratır (2FA disabled).</summary>
    private async Task<(string Email, string Password)> SeedSystemUserWithout2FaAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@cleantenant.test";
        const string password = "PreAuthTest-2026!";

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // System scope'ta Developer rolü zaten WebApiFactoryFixture.SeedAsync'te var
        var role = db.Roles.AsQueryable()
            .First(r => r.Name == "Developer" && r.Scope == ScopeLevel.System);

        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "PreAuth",
            LastName = "Test",
        };
        var createResult = await userManager.CreateAsync(user, password);
        createResult.Succeeded.Should().BeTrue();

        // 2FA enable EDİLMEZ — login bunu görüp EnrollmentRequired döndürmeli
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = role.Id,
            ScopeLevel = ScopeLevel.System,
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        return (email, password);
    }

    /// <summary>Login çağırır, EnrollmentRequired challenge token döner.</summary>
    private async Task<(HttpClient Client, Guid ChallengeToken, string Email)> BeginEnrollmentAsync(string prefix)
    {
        var (email, password) = await SeedSystemUserWithout2FaAsync(prefix);
        var client = _fixture.CreateClient();

        var loginBody = new
        {
            identifier = email,
            password,
            persona = "Management",
            contextId = (Guid?)null,
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginBody);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LoginShape>();
        result!.Status.Should().Be("EnrollmentRequired");
        return (client, result.EnrollmentChallenge!.ChallengeToken, email);
    }

    /// <summary>
    /// RFC 6238 TOTP — Base32 secret + UTC time / 30s counter + HMAC-SHA1.
    /// ASP.NET Identity'nin AuthenticatorTokenProvider.ValidateAsync ile uyumlu.
    /// </summary>
#pragma warning disable CA5350 // RFC 6238 TOTP HMAC-SHA1 zorunlu (RFC standardı + RFC 4226 uyumlu authenticator app'leri)
    private static string GenerateTotp(string base32Secret)
    {
        var key = Base32Decode(base32Secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30L;
        Span<byte> counterBytes = stackalloc byte[8];
        for (int i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        Span<byte> hash = stackalloc byte[20];
        hmac.TryComputeHash(counterBytes, hash, out _);

        int offset = hash[^1] & 0x0F;
        int binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }
#pragma warning restore CA5350

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var clean = base32.TrimEnd('=').ToUpperInvariant();
        var result = new List<byte>(clean.Length * 5 / 8);
        int bitBuffer = 0;
        int bitsLeft = 0;
        foreach (var c in clean)
        {
            int idx = alphabet.IndexOf(c);
            if (idx < 0) continue;
            bitBuffer = (bitBuffer << 5) | idx;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                result.Add((byte)(bitBuffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }
        return [.. result];
    }

    private sealed record LoginShape(
        string Status,
        object? Tokens,
        object? Challenge,
        EnrollmentChallengeShape? EnrollmentChallenge);

    private sealed record EnrollmentChallengeShape(
        Guid ChallengeToken,
        DateTimeOffset ExpiresAt,
        string Email);

    private sealed record StartShape(string Email, string Secret, string QrCodeUri);

    private sealed record CompleteShape(IReadOnlyList<string> RecoveryCodes);

    private sealed record TokenShape(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        Guid SessionId,
        Guid ContextId);
}
