using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace CleanTenant.ManagementApp.Auth;

/// <summary>
/// v0.2.2 — gerçek auth state provider. Cookie auth scheme'inden gelen
/// <see cref="HttpContext.User"/>'ı Blazor cascading auth state olarak sunar.
/// Login işlemi <c>/auth/sign-in</c> endpoint'inde gerçekleştirilir;
/// <c>HttpContext.SignInAsync</c> cookie set'ler, sonraki request'lerde
/// User otomatik dolu gelir.
/// </summary>
internal sealed class JwtCookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>DI'dan HttpContext accessor'ı alır.</summary>
    public JwtCookieAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var http = _httpContextAccessor.HttpContext;
        var principal = http?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(new AuthenticationState(principal));
        }
        return Task.FromResult(Anonymous);
    }

    /// <summary>
    /// Login/Logout endpoint'lerinden çağrılır — cascading state'i refresh eder.
    /// (Blazor Server SignalR circuit'i state'i otomatik yenilemez; manuel notify.)
    /// </summary>
    public void NotifyStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
