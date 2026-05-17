namespace CleanTenant.WebApi.Endpoints;

/// <summary>Health check endpoint'leri.</summary>
public static class HealthEndpoints
{
    /// <summary>Sağlık endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }))
              .WithName("Health")
              .WithTags("Health");
        return routes;
    }
}
