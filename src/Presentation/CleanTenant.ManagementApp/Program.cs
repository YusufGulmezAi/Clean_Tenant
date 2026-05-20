using Blazored.LocalStorage;
using CleanTenant.Application;
using CleanTenant.Infrastructure.Caching;
using CleanTenant.Infrastructure.Export;
using CleanTenant.Infrastructure.Identity;
using CleanTenant.Infrastructure.Identity.Middleware;
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
var mainConnection = builder.Configuration.GetConnectionString("Main");

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
// v0.2.3.a — Main DB (tenant iş varlıkları). Companies sayfaları bu context'e bağlıdır.
if (!string.IsNullOrWhiteSpace(mainConnection))
{
    builder.Services.AddMainPersistence(mainConnection, auditConnection);
}

// ─── UI services ───
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// v0.2.4.b.4 — Localizer AddMudServices'den ÖNCE kayıt edilmeli: AddMudServices içindeki
// TryAddTransient<MudLocalizer> zaten kayıtlı gördüğünde skip eder → tek kayıt kalır.
// Sonra kayıt edilirse iki descriptor oluşur; bazı sürümlerde ilk descriptor kazanır → TR çalışmaz.
// v0.2.11.a — Scoped (önceden Transient): CleanTenantMudLocalizer artık scoped
// IStringLocalizer (DbStringLocalizer) inject ediyor; transient olarak kalsaydı
// captive dependency uyarısı çıkar ve scope mismatch'leri tetiklerdi.
builder.Services.AddScoped<MudBlazor.MudLocalizer, CleanTenantMudLocalizer>();
builder.Services.AddMudServices();
// v0.2.4.a — Excel (ClosedXML) + PDF (QuestPDF Community) export servisleri.
builder.Services.AddCleanTenantExport();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IThemeService, LocalStorageThemeService>();
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Localization/Resources");

// v0.2.4.b.4 — Desteklenen diller; kültür cookie'si (.AspNetCore.Culture) üzerinden seçilir.
// Varsayılan TR; diğer dillerde MudBlazor İngilizce fallback'ine düşer (tam çeviri ilerleyen fazda).
var supportedCultures = new[] { "tr-TR", "en-US", "de-DE", "ru-RU", "ar-SA" };
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(opts =>
{
    opts.SetDefaultCulture("tr-TR")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    opts.FallBackToParentCultures = true;
    opts.FallBackToParentUICultures = true;
});

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

// v0.2.9 — Catalog seeder her startup'ta idempotent çalışır.
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<CleanTenant.Infrastructure.Persistence.Seeding.CatalogSeeder>();
    await seeder.SeedCoreCatalogAsync();
}

// v0.2.10 — Lokalizasyon: önce DB'ye varsayılan çeviri kayıtlarını ekle,
// sonra in-memory store'a yükle. Singleton store sync indexer'lı IStringLocalizer
// için DB-roundtrip'siz lookup sağlar.
using (var scope = app.Services.CreateScope())
{
    var locSeeder = scope.ServiceProvider.GetRequiredService<CleanTenant.Infrastructure.Persistence.Seeding.LocalizationSeeder>();
    await locSeeder.SeedAsync();

    var locStore = app.Services.GetRequiredService<CleanTenant.Infrastructure.Persistence.Localization.LocalizationStore>();
    await locStore.ReloadAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRequestLocalization();
app.UseAuthentication();
// v0.2.3.b — Cookie auth doğrulamasından sonra Redis session lookup.
// sid claim'inden (AuthEndpoints.SignInWithSessionAsync ekledi) AuthSession
// Redis'ten çekilir + HttpUserContext.Current set edilir. SwitchTenantCommand /
// SwitchToSystemCommand handler'ları bu sayede ICurrentSessionAccessor.Current'a
// erişebilir; aksi takdirde InvalidOperationException fırlatılır.
app.UseSessionLookup();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
