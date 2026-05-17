using Blazored.LocalStorage;
using Bunit.JSInterop;
using CleanTenant.ManagementApp.Services;
using CleanTenant.ManagementApp.Themes;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary>
/// MudBlazor render eden component'lar için ortak <see cref="TestContext"/>:
/// gerekli servisleri kayıt eder + JSInterop'u loose mode'a alır.
/// </summary>
public abstract class MudTestContextBase : TestContext
{
    /// <summary>Mock <see cref="IThemeService"/> — testlere göre özelleştirilebilir.</summary>
    protected IThemeService ThemeServiceMock { get; }

    protected MudTestContextBase()
    {
        // MudBlazor JS interop gerek — loose mode'da bilinmeyen çağrılar görmezden gelinir
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddMudServices();

        // ThemeService mock (default: KurumsalMavi + light)
        ThemeServiceMock = Substitute.For<IThemeService>();
        ThemeServiceMock.CurrentPreset.Returns(ThemePresetId.KurumsalMavi);
        ThemeServiceMock.IsDarkMode.Returns(false);
        Services.AddScoped(_ => ThemeServiceMock);

        // Blazored.LocalStorage mock (LocalStorageThemeService doğrudan testlerinde
        // gerekli; component testlerinde IThemeService mock'lu)
        Services.AddScoped(_ => Substitute.For<ILocalStorageService>());

        // Anonim auth state (CascadingAuthenticationState için)
        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();
        Services.AddScoped<AuthenticationStateProvider>(_ =>
        {
            var stub = Substitute.For<AuthenticationStateProvider>();
            stub.GetAuthenticationStateAsync()
                .Returns(Task.FromResult(new AuthenticationState(
                    new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity()))));
            return stub;
        });
    }
}
