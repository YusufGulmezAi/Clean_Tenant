using CleanTenant.Application.Common.MultiTenancy;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.Infrastructure.Persistence.Context;
using CleanTenant.Infrastructure.Persistence.Interceptors;
using CleanTenant.Infrastructure.Persistence.MultiTenancy;
using CleanTenant.Infrastructure.Persistence.Seeding;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Identifiers;
using CleanTenant.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    /// <returns>Chain için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddCatalogPersistence(
        this IServiceCollection services,
        string connectionString)
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

        // ---- DbContext ----
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            options
                .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(CatalogDbContext).Assembly.GetName().Name))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditingInterceptor>(),
                    sp.GetRequiredService<UrlCodeGeneratingInterceptor>());
        });

        services.AddScoped<ICatalogDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

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

                // Kilitleme
                opts.Lockout.MaxFailedAccessAttempts = 5;
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

        return services;
    }
}
