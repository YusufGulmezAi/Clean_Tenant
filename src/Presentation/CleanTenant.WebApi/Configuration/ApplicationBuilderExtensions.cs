using CleanTenant.Infrastructure.Identity.Middleware;

namespace CleanTenant.WebApi.Configuration;

/// <summary>
/// CleanTenant'a özgü middleware pipeline kompozisyonu.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Auth pipeline: JWT bearer authentication → Redis session lookup → authorization.
    /// Bu sıralama kritik; her middleware bir öncekinin sonucuna güvenir.
    /// </summary>
    public static WebApplication UseCleanTenantPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseSessionLookup();
        app.UseAuthorization();

        return app;
    }
}
