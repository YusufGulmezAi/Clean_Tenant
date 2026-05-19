using CleanTenant.Application;
using CleanTenant.Infrastructure.Caching;
using CleanTenant.Infrastructure.Identity;
using CleanTenant.Infrastructure.Identity.Middleware;
using CleanTenant.Infrastructure.Logging;
using CleanTenant.Infrastructure.Persistence;
using CleanTenant.PortalApp.Auth;
using CleanTenant.PortalApp.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── JWT_* env → Jwt:* section mapping ───
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

builder.AddCleanTenantSerilog();

// ─── Connection strings ───
var catalogConnection = builder.Configuration.GetConnectionString("Catalog")
    ?? throw new InvalidOperationException("ConnectionStrings:Catalog bulunamadı.");
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis bulunamadı.");
var auditConnection = builder.Configuration.GetConnectionString("Audit");
var logConnection = builder.Configuration.GetConnectionString("Log");
var mainConnection = builder.Configuration.GetConnectionString("Main");

// ─── Backend services ───
builder.Services.AddApplicationServices();
builder.Services.AddCatalogPersistence(catalogConnection, auditConnection);
builder.Services.AddRedisCache(redisConnection);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCleanTenantNotifications(builder.Configuration, builder.Environment);
if (!string.IsNullOrWhiteSpace(auditConnection))
    builder.Services.AddAuditPersistence(auditConnection);
if (!string.IsNullOrWhiteSpace(logConnection))
    builder.Services.AddLogPersistence(logConnection);
if (!string.IsNullOrWhiteSpace(mainConnection))
    builder.Services.AddMainPersistence(mainConnection, auditConnection);

// ─── UI services ───
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Localization/Resources");

// ─── Cookie auth (Portal scheme) ───
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opts.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(opts =>
{
    opts.Cookie.Name = "cleantenant.portal";
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
app.UseSessionLookup();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapPortalAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
