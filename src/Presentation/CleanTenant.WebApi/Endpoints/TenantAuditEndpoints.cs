using CleanTenant.Application.Features.Tenant.GetTenantSupportAccessHistory;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Tenant Admin denetim endpoint'leri. v0.1.6'dan itibaren <see cref="IMediator"/>.
/// </summary>
public static class TenantAuditEndpoints
{
    /// <summary>Tenant audit endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapTenantAuditEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/tenant/audit").WithTags("TenantAudit");

        group.MapGet("/support-access", GetTenantSupportAccessAsync)
             .RequireAuthorization(AuthorizationPolicies.TenantScope);

        return routes;
    }

    private static async Task<IResult> GetTenantSupportAccessAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetTenantSupportAccessQuery(from, to, page, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static int MapErrorTypeToStatus(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Failure => StatusCodes.Status422UnprocessableEntity,
        ErrorType.Critical => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest,
    };
}
