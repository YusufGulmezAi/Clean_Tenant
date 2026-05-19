using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace CleanTenant.PortalApp.Auth;

/// <summary>Cookie auth scheme'inden gelen HttpContext.User'ı Blazor auth state olarak sunar.</summary>
internal sealed class JwtCookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCookieAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var http = _httpContextAccessor.HttpContext;
        var principal = http?.User;
        if (principal?.Identity?.IsAuthenticated == true)
            return Task.FromResult(new AuthenticationState(principal));
        return Task.FromResult(Anonymous);
    }
}
