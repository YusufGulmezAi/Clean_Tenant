using CleanTenant.Application.Common.Auth;
using CleanTenant.Infrastructure.Identity.Context;
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
            await Reject(context, "invalid_session_claim", "Geçersiz session claim'i.");
            return;
        }

        var session = await sessionStore.GetAsync(sessionId, context.RequestAborted);
        if (session is null)
        {
            await Reject(context, "session_revoked_or_expired", "Oturum revoke edilmiş veya süresi dolmuş.");
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
    /// 401 yanıtı yazar. <paramref name="code"/> ASCII-safe makine kodu (header'a),
    /// <paramref name="message"/> Türkçe açıklama (response body'sine).
    /// </summary>
    private static async Task Reject(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.Append("X-Auth-Failure-Code", code);
        await context.Response.WriteAsync(message);
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
