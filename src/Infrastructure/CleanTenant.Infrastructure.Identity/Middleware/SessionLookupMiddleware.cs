using System.Security.Claims;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.Infrastructure.Identity.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CleanTenant.Infrastructure.Identity.Middleware;

/// <summary>
/// <para>
/// JWT bearer auth middleware'in BAŞARILI doğrulamasından sonra çalışır.
/// JWT'deki <c>sid</c> claim'inden Redis lookup yapar; session yoksa
/// <b>401 Unauthorized</b> döner (revoked / TTL doldu). Varsa
/// <see cref="HttpUserContext.Current"/>'i doldurur ve sliding TTL ile session'ı
/// yeniler.
/// </para>
/// </summary>
public sealed class SessionLookupMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtSettings _jwtSettings;
    private readonly SessionSettings _sessionSettings;

    /// <summary>DI tarafından çağrılır.</summary>
    public SessionLookupMiddleware(
        RequestDelegate next,
        IOptions<JwtSettings> jwtOptions,
        IOptions<SessionSettings> sessionOptions)
    {
        _next = next;
        _jwtSettings = jwtOptions.Value;
        _sessionSettings = sessionOptions.Value;
    }

    /// <summary>Pipeline ortasında çağrılır.</summary>
    public async Task InvokeAsync(
        HttpContext context,
        IAuthSessionStore sessionStore,
        ISessionFreshener sessionFreshener,
        HttpUserContext userContext)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var sidClaim = context.User.FindFirst(JwtClaimNames.SessionId)?.Value;
        if (string.IsNullOrEmpty(sidClaim) || !Guid.TryParse(sidClaim, out var sessionId))
        {
            if (await HandleMissingSessionAsync(context, "invalid_session_claim", "Geçersiz session claim'i."))
            {
                await _next(context);
            }
            return;
        }

        // GetFreshAsync: authorization damgası bayatsa izinleri yeniden çözüp oturumu
        // tazeler → yeni yetkiler re-login gerektirmeden bu istekte geçerli olur.
        var session = await sessionFreshener.GetFreshAsync(sessionId, context.RequestAborted);
        if (session is null)
        {
            if (await HandleMissingSessionAsync(context, "session_revoked_or_expired", "Oturum revoke edilmiş veya süresi dolmuş."))
            {
                await _next(context);
            }
            return;
        }

        userContext.Current = session;

        // Sliding TTL: session'ı yenile
        var ttl = TimeSpan.FromMinutes(
            _jwtSettings.AccessTokenMinutes + _sessionSettings.TtlPaddingMinutes);
        await sessionStore.TouchAsync(sessionId, ttl, context.RequestAborted);

        await _next(context);
    }

    /// <summary>
    /// Session bulunamadığında çağrılır. Davranış kullanılan auth scheme'ine göre değişir:
    /// <list type="bullet">
    ///   <item><b>Cookie auth (Blazor / Razor sayfa):</b> auth cookie'sini sil (revoke),
    ///   ClaimsPrincipal'i anonymize et, pipeline'a devam — kimliği gerektiren endpoint'lerde
    ///   ASP.NET Authorization handler 401 → Cookie middleware <c>LoginPath</c>'e yönlendirir.
    ///   Anonim path'lerde (Login.razor, auth endpoint'leri) sayfa normal render olur.</item>
    ///   <item><b>JWT bearer auth (WebApi):</b> 401 + X-Auth-Failure-Code header. İstemci
    ///   refresh token akışıyla yeni token alır.</item>
    /// </list>
    /// <returns>true → caller _next çağırmalı (cookie scheme); false → response yazıldı, halt.</returns>
    /// </summary>
    private static async Task<bool> HandleMissingSessionAsync(HttpContext context, string code, string message)
    {
        var isCookieAuth = context.User.Identities.Any(i =>
            string.Equals(i.AuthenticationType, CookieAuthenticationDefaults.AuthenticationScheme, StringComparison.Ordinal));

        if (isCookieAuth)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            context.Response.Headers.Append("X-Auth-Failure-Code", code);
            return true; // caller _next çağıracak
        }

        // JWT bearer: 401 + body, pipeline durur.
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.Append("X-Auth-Failure-Code", code);
        await context.Response.WriteAsync(message);
        return false;
    }
}

/// <summary>
/// <see cref="SessionLookupMiddleware"/>'i pipeline'a eklemek için
/// extension method.
/// </summary>
public static class SessionLookupMiddlewareExtensions
{
    /// <summary>Session lookup middleware'ini pipeline'a ekler.</summary>
    public static IApplicationBuilder UseSessionLookup(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SessionLookupMiddleware>();
    }
}
