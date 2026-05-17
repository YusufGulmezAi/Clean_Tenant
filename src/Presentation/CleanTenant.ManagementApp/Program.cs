using Blazored.LocalStorage;
using CleanTenant.Application;
using CleanTenant.Infrastructure.Caching;
using CleanTenant.Infrastructure.Identity;
using CleanTenant.Infrastructure.Logging;
using CleanTenant.Infrastructure.Persistence;
using CleanTenant.ManagementApp.Auth;
using CleanTenant.ManagementApp.Components;
using CleanTenant.ManagementApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── JWT_* env → Jwt:* section mapping (WebApi pattern; pragmatik duplicate, Faz 1.2'de paylaşılır) ───
var envMappings = new Dictionary<string, string?>
{
    ["Jwt:Issuer"] = builder.Configuration["JWT_ISSUER"],
    ["Jwt:Audience"] = builder.Configuration["JWT_AUDIENCE"],
    ["Jwt:SigningKey"] = builder.Configuration["JWT_SIGNING_KEY"],
    ["Jwt:AccessTokenMinutes"] = builder.Configuration["JWT_ACCESS_TOKEN_MINUTES"],
    ["Jwt:RefreshTokenDays"] = builder.Configuration["JWT_REFRESH_TOKEN_DAYS"],
    ["Session:TtlPaddingMinutes"] = builder.Configuration["SESSION_TTL_PADDING_MINUTES"],
};
builder.Configuration.AddInMemoryCollection(envMappings);

// ─── Serilog (v0.1.7'den itibaren) ───
builder.AddCleanTenantSerilog();

// ─── Connection strings ───
var catalogConnection = builder.Configuration.GetConnectionString("Catalog")
    ?? throw new InvalidOperationException("ConnectionStrings:Catalog bulunamadı.");
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis bulunamadı.");
var auditConnection = builder.Configuration.GetConnectionString("Audit");
var logConnection = builder.Configuration.GetConnectionString("Log");

// ─── Backend services — WebApi ile aynı pipeline (in-process IMediator) ───
builder.Services.AddApplicationServices();
builder.Services.AddCatalogPersistence(catalogConnection, auditConnection);
builder.Services.AddRedisCache(redisConnection);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCleanTenantNotifications(builder.Configuration, builder.Environment);
if (!string.IsNullOrWhiteSpace(auditConnection))
{
    builder.Services.AddAuditPersistence(auditConnection);
}
if (!string.IsNullOrWhiteSpace(logConnection))
{
    builder.Services.AddLogPersistence(logConnection);
}

// ─── UI services ───
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IThemeService, LocalStorageThemeService>();
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Localization/Resources");

// ─── Auth — Cookie scheme (Blazor Server için) ───
// AddIdentityServices içinde JWT Bearer zaten kayıt edildi (WebApi için).
// ManagementApp default scheme'i Cookie olarak override eder — Blazor sayfa
// erişimleri cookie ile authenticate. JWT Bearer scheme'i hâlâ kayıtlı ama
// kullanılmaz (challenge cookie üzerinden).
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opts.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(opts =>
{
    opts.Cookie.Name = "cleantenant.auth";
    opts.Cookie.HttpOnly = true;
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Strict;
    opts.LoginPath = "/login";
    opts.LogoutPath = "/login";
    opts.AccessDeniedPath = "/access-denied";
    opts.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    opts.SlidingExpiration = true;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtCookieAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
