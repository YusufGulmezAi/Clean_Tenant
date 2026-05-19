namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Tüm CleanTenant WebApi endpoint gruplarını tek çağrıda route'a bağlar.
/// Yeni endpoint grubu eklendikçe buraya tek satırla eklenir.
/// </summary>
public static class EndpointMappingExtensions
{
    /// <summary>Health + Auth + ileride diğer tüm endpoint gruplarını mapler.</summary>
    public static WebApplication MapCleanTenantEndpoints(this WebApplication app)
    {
        app.MapHealthEndpoints();
        app.MapAuthEndpoints();
        app.MapTwoFactorEndpoints();
        app.MapUserAdminEndpoints();
        app.MapSystemEndpoints();
        app.MapSystemLocalizationEndpoints();
        app.MapTenantAuditEndpoints();
        app.MapCompanyEndpoints();
        app.MapRoleEndpoints();
        return app;
    }
}
