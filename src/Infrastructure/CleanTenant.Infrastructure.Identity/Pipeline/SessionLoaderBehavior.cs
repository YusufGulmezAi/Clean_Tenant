using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.Infrastructure.Identity.Context;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CleanTenant.Infrastructure.Identity.Pipeline;

/// <summary>
/// <para>
/// <b>Pipeline'ın en başında</b> çalışan behavior. Normal HTTP request'lerinde
/// <c>SessionLookupMiddleware</c> <see cref="HttpUserContext.Current"/>'i
/// önceden doldurur ve bu behavior boştur. Ancak Blazor Server SignalR
/// circuit'inde Razor component event'leri yeni DI scope'unda çalışır;
/// middleware bu scope'ta tetiklenmediği için HttpUserContext null kalır.
/// </para>
/// <para>
/// Bu davranış o boşluğu doldurur: HttpContext'in <c>User</c>'ından
/// <c>sid</c> claim'ini okur ve Redis'ten async olarak session yükler.
/// Async — Blazor Server SynchronizationContext'inde deadlock yapmaz.
/// </para>
/// <para>
/// Registration: Identity DI içinde <c>services.Insert(0, ...)</c> ile
/// <b>en başa eklenir</b>, böylece Application katmanındaki diğer
/// behavior'lardan (AuthorizationBehavior dahil) önce çalışır.
/// </para>
/// </summary>
public sealed class SessionLoaderBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISessionFreshener _sessionFreshener;
    private readonly HttpUserContext _userContext;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SessionLoaderBehavior(
        IHttpContextAccessor httpContextAccessor,
        ISessionFreshener sessionFreshener,
        HttpUserContext userContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessionFreshener = sessionFreshener;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Zaten yüklü (middleware veya Blazor circuit scope'u). Blazor Server'da
        // HttpUserContext circuit boyunca kalıcı olduğundan, izin değişimini yakalamak
        // için her istekte damgayı kontrol edip bayatsa tazelemeliyiz.
        var current = _userContext.Current;
        if (current is not null)
        {
            var fresh = await _sessionFreshener.GetFreshAsync(current.SessionId, cancellationToken);
            if (fresh is not null)
            {
                _userContext.Current = fresh;
            }
            return await next();
        }

        var http = _httpContextAccessor.HttpContext;
        if (http?.User.Identity?.IsAuthenticated == true)
        {
            var sidClaim = http.User.FindFirst(JwtClaimNames.SessionId)?.Value;
            if (!string.IsNullOrEmpty(sidClaim) && Guid.TryParse(sidClaim, out var sessionId))
            {
                // GetFreshAsync: damga bayatsa izinleri yeniden çözüp oturumu tazeler
                // (re-login gerektirmeden yeni yetkiler bu istekte geçerli olur).
                var session = await _sessionFreshener.GetFreshAsync(sessionId, cancellationToken);
                if (session is not null)
                {
                    _userContext.Current = session;
                }
            }
        }

        return await next();
    }
}
