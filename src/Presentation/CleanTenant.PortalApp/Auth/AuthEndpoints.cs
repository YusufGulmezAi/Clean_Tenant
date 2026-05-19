using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Application.Features.Auth.Logout;
using CleanTenant.Application.Features.Auth.TwoFactor.SendCode;
using CleanTenant.Application.Features.Auth.TwoFactor.VerifyTwoFactor;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.PortalApp.Auth;

/// <summary>Portal Blazor Server için cookie set/sil endpoint'leri.</summary>
public static class AuthEndpoints
{
    internal const string SidClaim = "sid";
    internal const string UserIdClaim = "user_id";
    internal const string ContextIdClaim = "ctx";
    internal const string ScopeClaim = "scope";
    internal const string TenantIdClaim = "tenant_id";
    internal const string TenantNameClaim = "tenant_name";
    internal const string CompanyIdClaim = "company_id";
    internal const string CompanyNameClaim = "company_name";

    private const string ChallengeCookieName = "__ct_portal_2fa_chal";

    private static readonly CookieOptions ChallengeCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(5),
        IsEssential = true,
    };

    private static readonly CookieOptions DeleteCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
    };

    /// <summary>Portal auth endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapPortalAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/auth/sign-in", SignInAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        routes.MapPost("/auth/sign-out", SignOutAsync)
              .DisableAntiforgery();

        routes.MapGet("/auth/sign-out", SignOutAsync);

        routes.MapPost("/auth/2fa/verify", Verify2FaAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        routes.MapPost("/auth/2fa/send-code", Send2FaCodeAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        return routes;
    }

    private static async Task<IResult> SignInAsync(
        HttpContext httpContext,
        [FromForm] string identifier,
        [FromForm] string password,
        [FromForm] bool? rememberMe,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var remember = rememberMe ?? false;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new LoginCommand(identifier, password, PersonaSide.Portal, ContextId: null, ip, ua);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
            return Results.Redirect($"/login?error={Uri.EscapeDataString(result.FirstError.Code)}");

        var login = result.Value!;
        if (login.Status == LoginStatus.TwoFactorRequired)
        {
            var token = login.Challenge!.ChallengeToken;
            httpContext.Response.Cookies.Append(ChallengeCookieName, token.ToString("N"), ChallengeCookieOptions);
            return Results.Redirect("/2fa/challenge");
        }

        await SignInWithSessionAsync(httpContext, login.Tokens!, remember);
        return Results.Redirect("/");
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
        var result = await mediator.Send(new VerifyTwoFactorCommand(challengeToken, method, code, ip, ua), cancellationToken);

        if (result.IsFailure)
        {
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
        httpContext.Response.Cookies.Append(ChallengeCookieName, challengeToken.ToString("N"), ChallengeCookieOptions);

        if (result.IsFailure)
            return Results.Redirect($"/2fa/challenge?error={Uri.EscapeDataString(result.FirstError.Code)}");

        return Results.Redirect($"/2fa/challenge?info={Uri.EscapeDataString("Kod gönderildi.")}");
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new LogoutCommand(), cancellationToken);
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    private static bool TryReadChallengeCookie(HttpContext httpContext, out Guid challengeToken)
    {
        challengeToken = Guid.Empty;
        var raw = httpContext.Request.Cookies[ChallengeCookieName];
        if (string.IsNullOrEmpty(raw)) return false;
        return Guid.TryParseExact(raw, "N", out challengeToken) || Guid.TryParse(raw, out challengeToken);
    }

    internal static async Task SignInWithSessionAsync(HttpContext httpContext, TokenPair tokens, bool rememberMe)
    {
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwt = jwtHandler.ReadJwtToken(tokens.AccessToken);
        var userIdValue = jwt.Subject;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userIdValue),
            new(UserIdClaim, userIdValue),
            new(SidClaim, tokens.SessionId.ToString("N")),
            new(ContextIdClaim, tokens.ContextId.ToString("N")),
            new(ScopeClaim, tokens.CurrentScope.Level.ToString()),
        };

        if (tokens.CurrentScope.TenantId is { } tenantId)
        {
            claims.Add(new Claim(TenantIdClaim, tenantId.ToString("N")));
            if (!string.IsNullOrEmpty(tokens.CurrentScope.TenantName))
                claims.Add(new Claim(TenantNameClaim, tokens.CurrentScope.TenantName));
        }
        if (tokens.CurrentScope.CompanyId is { } companyId)
        {
            claims.Add(new Claim(CompanyIdClaim, companyId.ToString("N")));
            if (!string.IsNullOrEmpty(tokens.CurrentScope.CompanyName))
                claims.Add(new Claim(CompanyNameClaim, tokens.CurrentScope.CompanyName));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null,
            AllowRefresh = true,
        };

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}
