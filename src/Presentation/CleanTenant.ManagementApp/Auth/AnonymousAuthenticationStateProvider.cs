using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace CleanTenant.ManagementApp.Auth;

/// <summary>
/// v0.2.1 — geçici anonymous auth state provider. v0.2.2'de gerçek login
/// akışıyla (JWT cookie) değiştirilecek. <see cref="AuthenticationStateProvider"/>
/// üzerinden cascading <c>AuthenticationState</c> sağlar — sayfalar
/// <c>&lt;AuthorizeView&gt;</c> kullanabilsin.
/// </summary>
internal sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> AnonymousState =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => AnonymousState;
}
