using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Application.Features.Auth.Logout;
using CleanTenant.Application.Features.Auth.LogoutAllSessions;
using CleanTenant.Application.Features.Auth.Refresh;
using CleanTenant.Application.Features.Auth.SwitchContext;
using CleanTenant.Application.Features.System.ElevateToWrite;
using CleanTenant.Application.Features.System.EnterSupportMode;
using CleanTenant.Application.Features.System.ExitSupportMode;
using CleanTenant.Application.Features.System.ForceLogoutUser;
using CleanTenant.Application.Features.System.GetSystemSupportSessions;
using CleanTenant.Application.Features.System.ImpersonateUser;
using CleanTenant.Application.Features.System.RevokeSession;
using CleanTenant.Application.Features.Tenant.GetTenantSupportAccessHistory;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.Infrastructure.Identity.Context;
using CleanTenant.Infrastructure.Identity.Jwt;
using CleanTenant.Infrastructure.Identity.RefreshTokens;
using CleanTenant.SharedKernel.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Auth command handler'ları
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<SwitchContextCommandHandler>();
        services.AddScoped<LogoutAllSessionsCommandHandler>();
        services.AddScoped<ForceLogoutUserCommandHandler>();
        services.AddScoped<RevokeSessionCommandHandler>();

        // Support Mode handler'ları (System operatörlerin tenant'a giriş/çıkış akışı)
        services.AddScoped<EnterSupportModeCommandHandler>();
        services.AddScoped<ExitSupportModeCommandHandler>();
        services.AddScoped<ElevateToWriteCommandHandler>();
        services.AddScoped<ImpersonateUserCommandHandler>();
        services.AddScoped<GetTenantSupportAccessQueryHandler>();
        services.AddScoped<GetSystemSupportSessionsQueryHandler>();

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
}
