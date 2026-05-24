using CleanTenant.Application.Common.Auditing;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Identity;
using CleanTenant.Application.Common.Notifications;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Infrastructure.Identity.Auditing;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.Infrastructure.Identity.Context;
using CleanTenant.Infrastructure.Identity.Jwt;
using CleanTenant.Infrastructure.Identity.Notifications;
using CleanTenant.Infrastructure.Identity.Pipeline;
using CleanTenant.Infrastructure.Identity.RefreshTokens;
using CleanTenant.Infrastructure.Identity.Users;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CleanTenant.Infrastructure.Identity;

/// <summary>
/// Identity katmanının DI kayıtları: JWT, refresh token, HTTP-bound context,
/// auth command handler'lar, ASP.NET Core authentication + authorization policies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Identity servisleri + JWT bearer authentication + authorization policies.
    /// </summary>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SessionSettings>(configuration.GetSection(SessionSettings.SectionName));

        // JWT üretimi
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // HTTP-bound context (Persistence katmanı System* default'larını ezer)
        services.AddHttpContextAccessor();
        services.AddScoped<HttpUserContext>();
        services.AddScoped<IUserContext>(sp => sp.GetRequiredService<HttpUserContext>());
        services.AddScoped<ICurrentSessionAccessor>(sp => sp.GetRequiredService<HttpUserContext>());
        services.AddScoped<ITenantContext, HttpTenantContext>();

        // Arka plan job'ları için sentetik tenant oturumu çalıştırıcı (Hangfire).
        services.AddScoped<Application.Common.Jobs.ISystemJobExecutor, Jobs.SystemJobExecutor>();

        // SessionLoaderBehavior — Application'ın AuthorizationBehavior'undan ÖNCE
        // çalışmalı. AddApplicationServices() önce çağrıldığı için Insert(0) ile
        // services collection'ın başına ekliyoruz; MediatR IPipelineBehavior'ları
        // services collection'daki sırayla zincirler, böylece bu behavior pipeline
        // başında çalışır. Blazor Server SignalR scope'unda middleware'in
        // dolduramadığı HttpUserContext'i async olarak doldurur.
        services.Insert(0, ServiceDescriptor.Transient(
            typeof(IPipelineBehavior<,>),
            typeof(SessionLoaderBehavior<,>)));

        // Auth command handler'ları MediatR tarafından otomatik kayıt edilir
        // (AddApplicationServices). LoginFinalizer IRequestHandler değil — yardımcı
        // sınıf, manuel kayıt.
        services.AddScoped<LoginFinalizer>();

        // v0.1.6 — Permission checker (Redis session permission listesinden okur)
        services.AddScoped<IPermissionChecker, SessionPermissionChecker>();

        // v0.1.7 — Audit metadata accessor (HttpContext + session + UA parser)
        services.AddScoped<IAuditMetadataAccessor, HttpAuditMetadataAccessor>();

        // Kullanıcı yönetimi (UserManager sarmalayıcısı)
        services.AddScoped<IUserRepository, UserRepository>();

        // JWT bearer authentication
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings okunamadı.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = TokenValidationParametersFactory.Create(jwt);
                opts.MapInboundClaims = false;
            });

        // Authorization handler'ları (scoped — ICurrentSessionAccessor enjekte ediyorlar)
        services.AddScoped<IAuthorizationHandler, SystemScopeHandler>();
        services.AddScoped<IAuthorizationHandler, TenantScopeHandler>();
        services.AddScoped<IAuthorizationHandler, SupportModeActiveHandler>();
        services.AddScoped<IAuthorizationHandler, SupportWriteEnabledHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.SystemScope, p =>
                p.RequireAuthenticatedUser().AddRequirements(new SystemScopeRequirement()));
            options.AddPolicy(AuthorizationPolicies.TenantScope, p =>
                p.RequireAuthenticatedUser().AddRequirements(new TenantScopeRequirement()));
            options.AddPolicy(AuthorizationPolicies.SupportModeActive, p =>
                p.RequireAuthenticatedUser().AddRequirements(new SupportModeActiveRequirement()));
            options.AddPolicy(AuthorizationPolicies.SupportWriteEnabled, p =>
                p.RequireAuthenticatedUser().AddRequirements(new SupportWriteEnabledRequirement()));
        });

        return services;
    }

    /// <summary>
    /// E-posta + SMS sender'larını kayıt eder. Provider'lar
    /// <c>Email:Provider</c> ve <c>Sms:Provider</c> konfigürasyon anahtarlarından
    /// okunur. Şu an yalnız <c>Console</c> implementasyonu var; Faz 1'de SMTP /
    /// Twilio gibi gerçek implementasyonlar eklenecek.
    /// </summary>
    /// <remarks>
    /// <para>Production guard'ı:</para>
    /// <list type="bullet">
    ///   <item><c>Console</c> sender Production'da kayıt edilirse boot başarısız
    ///         olur — kullanıcının sessiz hata almaması için.</item>
    ///   <item>Diğer ortamlarda (Development / Test / Demo) provider belirtilmezse
    ///         <c>Console</c> default'tur.</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddCleanTenantNotifications(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        RegisterEmailSender(services, configuration, environment);
        RegisterSmsSender(services, configuration, environment);
        return services;
    }

    private static void RegisterEmailSender(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var provider = (configuration["Email:Provider"] ?? "Console").Trim();

        if (environment.IsProduction() &&
            string.Equals(provider, "Console", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Email:Provider Production ortamında 'Console' olamaz. " +
                "Gerçek bir sağlayıcı (SMTP / SendGrid vb.) belirtin.");
        }

        if (string.Equals(provider, "Console", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmailSender, ConsoleEmailSender>();
        }
        else
        {
            throw new InvalidOperationException(
                $"Bilinmeyen Email:Provider '{provider}'. v0.1.5.c yalnız 'Console' destekler.");
        }
    }

    private static void RegisterSmsSender(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var provider = (configuration["Sms:Provider"] ?? "Console").Trim();

        if (environment.IsProduction() &&
            string.Equals(provider, "Console", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Sms:Provider Production ortamında 'Console' olamaz. " +
                "Gerçek bir sağlayıcı (Twilio / NetGSM vb.) belirtin.");
        }

        if (string.Equals(provider, "Console", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ISmsSender, ConsoleSmsSender>();
        }
        else
        {
            throw new InvalidOperationException(
                $"Bilinmeyen Sms:Provider '{provider}'. v0.1.5.c yalnız 'Console' destekler.");
        }
    }
}
