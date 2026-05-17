using Blazored.LocalStorage;
using CleanTenant.ManagementApp.Auth;
using CleanTenant.ManagementApp.Components;
using CleanTenant.ManagementApp.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor Components + interactive server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// v0.2.1 — MudBlazor services + tema kalıcılığı için Blazored.LocalStorage
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IThemeService, LocalStorageThemeService>();

// v0.2.1 — Anonim auth state provider (v0.2.2'de gerçek login'le aktive edilir)
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();

// Localization scaffold (TR default; .resx tabanlı)
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Localization/Resources");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
