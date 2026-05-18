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

        return routes;
    }

    private static async Task<IResult> Verify2FaAsync(
        HttpContext httpContext,
        [FromForm] string token,
        [FromForm] string method,
        [FromForm] string code,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(token, "N", out var challengeToken) &&
            !Guid.TryParse(token, out challengeToken))
        {
            return Results.Redirect("/login?error=AUTH-2FA-CHALLENGE-NOT-FOUND");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new VerifyTwoFactorCommand(challengeToken, method, code, ip, ua);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return Results.Redirect($"/2fa/challenge?token={challengeToken:N}&error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);
        return Results.Redirect("/");
    }

    private static async Task<IResult> Send2FaCodeAsync(
        [FromForm] string token,
        [FromForm] string method,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(token, "N", out var challengeToken) &&
            !Guid.TryParse(token, out challengeToken))
        {
            return Results.Redirect("/login?error=AUTH-2FA-CHALLENGE-NOT-FOUND");
        }

        var result = await mediator.Send(new SendTwoFactorCodeCommand(challengeToken, method), cancellationToken);
        if (result.IsFailure)
        {
            return Results.Redirect($"/2fa/challenge?token={challengeToken:N}&error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        return Results.Redirect($"/2fa/challenge?token={challengeToken:N}&info={Uri.EscapeDataString("Kod gönderildi (Development: console log).")}");
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
            return Results.Redirect($"/2fa/challenge?token={token:N}");
        }

        // v0.2.2.a — System scope kullanıcısı + 2FA yok → pre-auth enrollment sayfasına
        if (login.Status == LoginStatus.EnrollmentRequired)
        {
            var token = login.EnrollmentChallenge!.ChallengeToken;
            return Results.Redirect($"/2fa/enroll-pre-auth?token={token:N}");
        }

        // Success → cookie set
        var tokens = login.Tokens!;
        await SignInWithSessionAsync(httpContext, tokens, remember);
        return Results.Redirect("/");
    }

    private static async Task<IResult> SwitchTenantFormAsync(
        HttpContext httpContext,
        [FromForm] Guid tenantId,
        [FromForm] string? returnUrl,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new SwitchTenantCommand(tenantId, ip, ua);

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

    private static async Task<IResult> FinalizePreAuthEnrollmentAsync(
        HttpContext httpContext,
        [FromForm] string token,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(token, "N", out var challengeToken) &&
            !Guid.TryParse(token, out challengeToken))
        {
            return Results.Redirect("/login?error=AUTH-2FA-ENROLL-CHALLENGE-NOT-FOUND");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();

        var result = await mediator.Send(
            new FinalizePreAuthEnrollmentCommand(challengeToken, ip, ua),
            cancellationToken);

        if (result.IsFailure)
        {
            return Results.Redirect($"/login?error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

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
