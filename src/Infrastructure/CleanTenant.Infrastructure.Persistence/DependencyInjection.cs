using CleanTenant.Application.Common.Auditing;
using CleanTenant.Application.Common.MultiTenancy;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.Persistence.Audit;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.Persistence.Catalog.Readers;
using CleanTenant.Infrastructure.Persistence.Context;
using CleanTenant.Infrastructure.Persistence.Interceptors;
using CleanTenant.Infrastructure.Persistence.Localization;
using CleanTenant.Infrastructure.Persistence.Log;
using CleanTenant.Infrastructure.Persistence.Main;
using CleanTenant.Infrastructure.Persistence.MultiTenancy;
using CleanTenant.Infrastructure.Persistence.Seeding;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Identifiers;
using CleanTenant.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.Persistence;

/// <summary>
/// <para>
/// Persistence katmanının DI kayıtlarını yapan extension method'lar.
/// Composition root (WebApi / ManagementApp / PortalApp / MigrationRunner)
/// burayı çağırarak DbContext'leri ve servisleri kayıt eder.
/// </para>
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Catalog DbContext'i ve ona bağlı servisleri (interceptor'lar, Identity,
    /// tenant connection factory, seeder'lar) kayıt eder.
    /// </summary>
    /// <param name="services">DI servis koleksiyonu.</param>
    /// <param name="connectionString">Catalog DB PostgreSQL bağlantı dizgesi.</param>
    /// <param name="auditConnectionString">
    /// v0.1.7 — opsiyonel Audit DB bağlantısı. Verildiyse <c>FullAuditInterceptor</c>
    /// kayıt edilir ve her Catalog SaveChanges audit DB'ye Dapper ile yazılır.
    /// </param>
    /// <returns>Chain için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddCatalogPersistence(
        this IServiceCollection services,
        string connectionString,
        string? auditConnectionString = null)
    {
        // ---- SharedKernel primitif'leri ----
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IUrlCodeGenerator, Base58UrlCodeGenerator>();

        // v0.1.4.b geçici placeholder'lar; v0.1.5'te HttpContext-bound impl ile değiştirilir.
        services.AddScoped<IUserContext, SystemUserContext>();
        services.AddScoped<ITenantContext, SystemTenantContext>();

        // ---- EF Core Interceptor'lar (scoped — DbContext başına çözümlenir) ----
        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<UrlCodeGeneratingInterceptor>();

        // v0.1.7 — FullAuditInterceptor (audit DB conn string verildiyse aktif).
        if (!string.IsNullOrWhiteSpace(auditConnectionString))
        {
            var auditConn = auditConnectionString;
            services.AddScoped<FullAuditInterceptor>(sp => new FullAuditInterceptor(
                sp.GetRequiredService<IAuditMetadataAccessor>(),
                sp.GetRequiredService<IClock>(),
                auditConn));
        }

        // ---- DbContext + DbContextFactory ----
        // v0.2.9 — IDbContextFactory eklendi: Blazor Server UI component'lerinin
        // (TenantSwitcher, RoleEditPage gibi) aynı circuit scope'unda paralel
        // DbContext kullanımı "second operation started" hatası veriyordu.
        // Reader'lar artık factory'den taze DbContext alıyor; command handler'lar
        // hâlâ scoped ICatalogDbContext kullanır (tek istek içinde sıralı).
        services.AddDbContextFactory<CatalogDbContext>((sp, options) =>
        {
            var interceptors = new List<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>
            {
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<UrlCodeGeneratingInterceptor>(),
            };
            if (!string.IsNullOrWhiteSpace(auditConnectionString))
            {
                interceptors.Add(sp.GetRequiredService<FullAuditInterceptor>());
            }

            options
                .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(CatalogDbContext).Assembly.GetName().Name))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .AddInterceptors(interceptors);
        }, ServiceLifetime.Scoped);

        // Scoped CatalogDbContext — factory üzerinden (command handler'lar için).
        services.AddScoped<CatalogDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<CatalogDbContext>>().CreateDbContext());
        services.AddScoped<ICatalogDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

        // ---- LookUp Catalog Reader (EF Core based) ----
        services.AddScoped<ILookUpCatalogReader, LookUpCatalogReader>();

        // ---- ASP.NET Core Identity entegrasyonu ----
        services.AddIdentityCore<User>(opts =>
            {
                // Şifre politikası — min 8 + sıkı complexity (büyük/küçük/rakam/özel)
                opts.Password.RequiredLength = 8;
                opts.Password.RequireUppercase = true;
                opts.Password.RequireLowercase = true;
                opts.Password.RequireDigit = true;
                opts.Password.RequireNonAlphanumeric = true;
                opts.Password.RequiredUniqueChars = 4;

                // Kullanıcı politikası
                opts.User.RequireUniqueEmail = true;

                // Kilitleme — ASP.NET Identity'nin yerleşik OTOMATİK kilidi devre dışı
                // bırakıldı (sentinel yüksek eşik). Kilit kararı artık tenant-başına
                // ayarlanabilir politikaya göre IAccountLockoutService içinde verilir
                // (bkz. AccountLockoutService). AccessFailedAsync yalnız sayacı artırır.
                // Global varsayılan (5 deneme / 15 dk) LockoutPolicy.Default'ta tutulur.
                opts.Lockout.AllowedForNewUsers = true;
                opts.Lockout.MaxFailedAccessAttempts = int.MaxValue;
                opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // SignIn — v0.1.5'te SignInManager devreye girdiğinde detaylandırılacak
                opts.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<Role>()
            .AddRoleManager<RoleManager<Role>>()
            .AddEntityFrameworkStores<CatalogDbContext>()
            // v0.1.5.c — 2FA için 3 sağlayıcı: AuthenticatorTokenProvider (TOTP),
            // EmailTokenProvider, PhoneNumberTokenProvider. Recovery code üretimini
            // de bu zincir sağlar (GenerateNewTwoFactorRecoveryCodesAsync).
            .AddDefaultTokenProviders();

        // ---- Multi-Tenancy ----
        services.AddMemoryCache();
        services.AddScoped<ITenantConnectionFactory, TenantConnectionFactory>();

        // ---- Seeder'lar ----
        services.AddScoped<CatalogSeeder>();
        services.AddScoped<DevSeedData>();
        services.AddScoped<DemoSeedData>();
        services.AddScoped<LocalizationSeeder>();
        services.AddScoped<LookUpSeeder>();

        // ---- v0.2.10 Lokalizasyon ----
        // LocalizationStore singleton — startup'ta DB'den preload edilir
        // (Program.cs'de). DbStringLocalizer scoped — IStringLocalizer'a bağlanır.
        services.AddSingleton<LocalizationStore>();
        services.AddScoped<Microsoft.Extensions.Localization.IStringLocalizer, DbStringLocalizer>();
        services.AddScoped<CleanTenant.Application.Common.Localization.ILocalizationCacheRefresher, LocalizationCacheRefresher>();

        return services;
    }

    /// <summary>
    /// Audit DB için EF Core DbContext kayıt eder. Yalnızca migration ve seyrek
    /// read senaryoları için; yazım <c>FullAuditInterceptor</c> üzerinden Dapper
    /// ile yapılır.
    /// </summary>
    public static IServiceCollection AddAuditPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AuditDbContext>(options =>
        {
            options
                .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(AuditDbContext).Assembly.GetName().Name))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        services.AddScoped<IAuditDbContext>(sp => sp.GetRequiredService<AuditDbContext>());
        return services;
    }

    /// <summary>
    /// Log DB için EF Core DbContext kayıt eder. Yalnız şema (migration) içindir;
    /// runtime'da Serilog PostgreSQL sink doğrudan <c>NpgsqlConnection</c> üzerinden yazar.
    /// </summary>
    public static IServiceCollection AddLogPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<LogDbContext>(options =>
        {
            options
                .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(LogDbContext).Assembly.GetName().Name))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        return services;
    }

    /// <summary>
    /// v0.2.3.a — Main DB için EF Core DbContext'i kayıt eder. Tenant iş varlıkları
    /// (Company, ileride Building/Unit/Invoice) buradadır. Global query filter
    /// <see cref="ITenantContext"/> üzerinden <c>tenant_id</c> izolasyonu uygular.
    /// </summary>
    /// <remarks>
    /// <para>Interceptor zinciri Catalog ile aynı: AuditingInterceptor +
    /// UrlCodeGeneratingInterceptor + (audit conn varsa) FullAuditInterceptor.</para>
    /// <para>Hibrit multi-tenancy: shared-mode default (HasDedicatedDatabase=false
    /// tenant'lar bu paylaşılan DB'ye yazar). Dedicated DB tenant'ları için
    /// runtime connection resolver Faz 1.X+'a ertelendi.</para>
    /// </remarks>
    public static IServiceCollection AddMainPersistence(
        this IServiceCollection services,
        string connectionString,
        string? auditConnectionString = null)
    {
        services.AddDbContextFactory<MainDbContext>((sp, options) =>
        {
            var interceptors = new List<IInterceptor>
            {
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<UrlCodeGeneratingInterceptor>(),
            };
            if (!string.IsNullOrWhiteSpace(auditConnectionString))
            {
                interceptors.Add(sp.GetRequiredService<FullAuditInterceptor>());
            }

            options
                .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(MainDbContext).Assembly.GetName().Name))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .AddInterceptors(interceptors);
        }, ServiceLifetime.Scoped);

        // Scoped MainDbContext — factory üzerinden (command handler'lar için).
        services.AddScoped<MainDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<MainDbContext>>().CreateDbContext());
        services.AddScoped<IMainDbContext>(sp => sp.GetRequiredService<MainDbContext>());

        // ---- Tahakkuk: hesap kodu üretici (Catalog + Main DB kullanır) ----
        services.AddScoped<Application.Features.Main.Accruals.IAccountCodeAllocator,
            Main.Accruals.AccountCodeAllocator>();

        return services;
    }
}
