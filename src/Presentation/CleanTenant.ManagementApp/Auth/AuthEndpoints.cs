using System.Security.Claims;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Application.Features.Auth.Logout;
using CleanTenant.Application.Features.Auth.Tenants;
using CleanTenant.Application.Features.Auth.TwoFactor.PreAuthEnrollment;
using CleanTenant.Application.Features.Auth.TwoFactor.SendCode;
using CleanTenant.Application.Features.Auth.TwoFactor.VerifyTwoFactor;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.ManagementApp.Auth;

/// <summary>
/// Blazor Server için cookie set/sil endpoint'leri. Razor form'ları buraya POST
/// eder — handler IMediator.Send ile backend'i çalıştırır ve HttpContext üzerinden
/// cookie set'ler veya siler.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>SessionLookupMiddleware'in Redis lookup için kullandığı JWT claim adı.</summary>
    public const string SidClaim = "sid";

    /// <summary>Sekme/persona context kimliği claim adı.</summary>
    public const string ContextIdClaim = "ctx";

    /// <summary>Aktif scope seviyesi claim adı (System/Tenant/Company/Unit).</summary>
    public const string ScopeClaim = "scope";

    // v0.2.2.d — 2FA challenge / enrollment token'ları artık query string yerine
    // kısa ömürlü HttpOnly cookie ile taşınır. Önceki davranışta token URL bar'da
    // görünüyordu: hem profesyonel olmayan görüntü, hem browser history / Referer
    // header / server log'larında leak. HttpOnly cookie XSS'e karşı da korur.
    internal const string ChallengeCookieName = "__ct_2fa_chal";
    internal const string EnrollmentCookieName = "__ct_2fa_enroll";

    // Challenge token backend'de 5 dk; cookie TTL'ini eşle.
    private static readonly CookieOptions ChallengeCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(5),
        IsEssential = true,
    };

    // Enrollment challenge backend'de 10 dk (UI'da yazılı).
    private static readonly CookieOptions EnrollmentCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(10),
        IsEssential = true,
    };

    // Cookie sil: Delete'in attribute'ları set ile birebir uyuşmalı, aksi halde
    // browser farklı cookie sayar ve eskisi kalır.
    private static readonly CookieOptions DeleteCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
    };

    /// <summary>Cookie auth endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        // v0.2.2 — Login form post handler. ManagementApp Login sayfası bunu form action olarak kullanır.
        routes.MapPost("/auth/sign-in", SignInAsync)
              .DisableAntiforgery() // form post; CSRF için Faz 1.2'de antiforgery token eklenir
              .AllowAnonymous();

        routes.MapPost("/auth/sign-out", SignOutAsync)
              .DisableAntiforgery();

        // v0.2.2 — MudMenuItem href'inden GET kabul edilir; Faz 1.X'te antiforgery POST-only.
        routes.MapGet("/auth/sign-out", SignOutAsync);

        routes.MapPost("/auth/2fa/verify", Verify2FaAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        routes.MapPost("/auth/2fa/send-code", Send2FaCodeAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        // v0.2.2.a — Pre-auth 2FA enrollment finalize.
        // Sayfa (TwoFactorEnrollmentPreAuth.razor) InteractiveServer modunda
        // Start + Complete'i IMediator ile in-process çağırır; finalize ise
        // cookie set'lemesi için HttpContext'e ulaşan bir endpoint olmalı —
        // bu yüzden form post pattern'i kullanılır (Login.razor gibi).
        routes.MapPost("/auth/2fa/enroll-pre-auth/finalize", FinalizePreAuthEnrollmentAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        // v0.2.3.b — AppBar "Aktif Tenant" dropdown form post.
        // SwitchTenantCommand çalıştırır, dönen TokenPair ile cookie'yi yeniler ve
        // kullanıcıyı belirtilen returnUrl'e (default "/") yönlendirir.
        routes.MapPost("/auth/switch-tenant", SwitchTenantFormAsync)
              .DisableAntiforgery()
              .RequireAuthorization();

        // v0.2.3.b — System scope'a geri dönüş.
        routes.MapPost("/auth/switch-to-system", SwitchToSystemFormAsync)
              .DisableAntiforgery()
              .RequireAuthorization();

        return routes;
    }

    private static async Task<IResult> Verify2FaAsync(
        HttpContext httpContext,
        [FromForm] string method,
        [FromForm] string code,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!TryReadChallengeCookie(httpContext, out var challengeToken))
        {
            httpContext.Response.Cookies.Delete(ChallengeCookieName, DeleteCookieOptions);
            return Results.Redirect("/login?error=AUTH-2FA-CHALLENGE-NOT-FOUND");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new VerifyTwoFactorCommand(challengeToken, method, code, ip, ua);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            // Cookie hâlâ geçerli — kullanıcı kodu tekrar deneyebilsin diye TTL'i yenile.
            httpContext.Response.Cookies.Append(ChallengeCookieName, challengeToken.ToString("N"), ChallengeCookieOptions);
            return Results.Redirect($"/2fa/challenge?error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        httpContext.Response.Cookies.Delete(ChallengeCookieName, DeleteCookieOptions);
        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);
        return Results.Redirect("/");
    }

    private static async Task<IResult> Send2FaCodeAsync(
        HttpContext httpContext,
        [FromForm] string method,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!TryReadChallengeCookie(httpContext, out var challengeToken))
        {
            httpContext.Response.Cookies.Delete(ChallengeCookieName, DeleteCookieOptions);
            return Results.Redirect("/login?error=AUTH-2FA-CHALLENGE-NOT-FOUND");
        }

        var result = await mediator.Send(new SendTwoFactorCodeCommand(challengeToken, method), cancellationToken);

        // Her iki dalda da cookie'yi tazele — kullanıcı kod girmeye devam edecek.
        httpContext.Response.Cookies.Append(ChallengeCookieName, challengeToken.ToString("N"), ChallengeCookieOptions);

        if (result.IsFailure)
        {
            return Results.Redirect($"/2fa/challenge?error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        return Results.Redirect($"/2fa/challenge?info={Uri.EscapeDataString("Kod gönderildi (Development: console log).")}");
    }

    private static bool TryReadChallengeCookie(HttpContext httpContext, out Guid challengeToken)
    {
        challengeToken = Guid.Empty;
        var raw = httpContext.Request.Cookies[ChallengeCookieName];
        if (string.IsNullOrEmpty(raw)) return false;
        return Guid.TryParseExact(raw, "N", out challengeToken) || Guid.TryParse(raw, out challengeToken);
    }

    private static bool TryReadEnrollmentCookie(HttpContext httpContext, out Guid challengeToken)
    {
        challengeToken = Guid.Empty;
        var raw = httpContext.Request.Cookies[EnrollmentCookieName];
        if (string.IsNullOrEmpty(raw)) return false;
        return Guid.TryParseExact(raw, "N", out challengeToken) || Guid.TryParse(raw, out challengeToken);
    }

    private static async Task<IResult> SignInAsync(
        HttpContext httpContext,
        [FromForm] string identifier,
        [FromForm] string password,
        [FromForm] string? persona,
        [FromForm] bool? rememberMe,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // HTML checkbox işaretsizse form'a hiç eklenmez — null gelir.
        var remember = rememberMe ?? false;
        var personaSide = (persona ?? "Management").Equals("Portal", StringComparison.OrdinalIgnoreCase)
            ? PersonaSide.Portal
            : PersonaSide.Management;

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new LoginCommand(identifier, password, personaSide, ContextId: null, ip, ua);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var error = result.FirstError;
            return Results.Redirect($"/login?error={Uri.EscapeDataString(error.Code)}");
        }

        var login = result.Value!;
        if (login.Status == LoginStatus.TwoFactorRequired)
        {
            var token = login.Challenge!.ChallengeToken;
            httpContext.Response.Cookies.Append(ChallengeCookieName, token.ToString("N"), ChallengeCookieOptions);
            return Results.Redirect("/2fa/challenge");
        }

        // v0.2.2.a — System scope kullanıcısı + 2FA yok → pre-auth enrollment sayfasına
        if (login.Status == LoginStatus.EnrollmentRequired)
        {
            var token = login.EnrollmentChallenge!.ChallengeToken;
            httpContext.Response.Cookies.Append(EnrollmentCookieName, token.ToString("N"), EnrollmentCookieOptions);
            return Results.Redirect("/2fa/enroll-pre-auth");
        }

        // Success → cookie set
        var tokens = login.Tokens!;
        await SignInWithSessionAsync(httpContext, tokens, remember);
        return Results.Redirect("/");
    }

    private static async Task<IResult> SwitchTenantFormAsync(
        HttpContext httpContext,
        [FromForm] Guid tenantId,
        [FromForm] Guid? companyId,
        [FromForm] string? returnUrl,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new SwitchTenantCommand(tenantId, companyId, ip, ua);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            // Hata durumunda kullanıcıyı geldiği yere geri yolla (error query ile)
            var fallback = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            var sep = fallback.Contains('?') ? '&' : '?';
            return Results.Redirect($"{fallback}{sep}switch-error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        // Önce eski cookie'yi sil (yeni session id ile değişti) sonra yeni cookie set
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);

        // Tenant değiştirildiğinde dashboard'a dön — hangi sayfada olduğu önemli değil
        // çünkü tenant-scoped sayfalarda data yenilenir (full reload).
        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    private static async Task<IResult> SwitchToSystemFormAsync(
        HttpContext httpContext,
        [FromForm] string? returnUrl,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var result = await mediator.Send(new SwitchToSystemCommand(ip, ua), cancellationToken);

        if (result.IsFailure)
        {
            var fallback = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            var sep = fallback.Contains('?') ? '&' : '?';
            return Results.Redirect($"{fallback}{sep}switch-error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);

        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    private static async Task<IResult> FinalizePreAuthEnrollmentAsync(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!TryReadEnrollmentCookie(httpContext, out var challengeToken))
        {
            httpContext.Response.Cookies.Delete(EnrollmentCookieName, DeleteCookieOptions);
            return Results.Redirect("/login?error=AUTH-2FA-ENROLL-CHALLENGE-NOT-FOUND");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();

        var result = await mediator.Send(
            new FinalizePreAuthEnrollmentCommand(challengeToken, ip, ua),
            cancellationToken);

        if (result.IsFailure)
        {
            // Finalize başarısız → cookie sil; kullanıcı baştan login akışına yönlensin.
            httpContext.Response.Cookies.Delete(EnrollmentCookieName, DeleteCookieOptions);
            return Results.Redirect($"/login?error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        httpContext.Response.Cookies.Delete(EnrollmentCookieName, DeleteCookieOptions);
        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);
        return Results.Redirect("/");
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Backend logout — Redis session sil + refresh token revoke
        await mediator.Send(new LogoutCommand(), cancellationToken);
        // Cookie sil
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    /// <summary>
    /// Tokens üzerinden cookie SignInAsync — claim'ler SessionLookupMiddleware
    /// için (sid, ctx, scope). "Beni hatırla" 7 gün TTL.
    /// </summary>
    internal static async Task SignInWithSessionAsync(
        HttpContext httpContext,
        TokenPair tokens,
        bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, tokens.SessionId.ToString()),
            new(SidClaim, tokens.SessionId.ToString("N")),
            new(ContextIdClaim, tokens.ContextId.ToString("N")),
            new(ScopeClaim, tokens.CurrentScope.Level.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : null,
            AllowRefresh = true,
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }
}
