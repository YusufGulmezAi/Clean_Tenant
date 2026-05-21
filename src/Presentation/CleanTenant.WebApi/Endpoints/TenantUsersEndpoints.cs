using CleanTenant.Application.Features.System.Users;
using CleanTenant.Application.Features.Tenant.Users;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Belirli bir Tenant'a ait kullanıcı yönetim endpoint'leri.
/// Sistem operatörü tarafından /system/tenants/{tenantId}/users üzerinden erişilir.
/// </summary>
public static class TenantUsersEndpoints
{
    /// <summary>Endpoint'leri route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapTenantUsersEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/v1/system/tenants/{tenantId}/users")
            .WithTags("Tenant.Users");

        group.MapGet("/", ListTenantUsersAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPost("/", CreateTenantUserAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        return routes;
    }

    private static async Task<IResult> ListTenantUsersAsync(
        Guid tenantId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null)
    {
        var query = new ListUsersQuery(ScopeLevel.Tenant, TenantId: tenantId, Search: search);
        var result = await mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> CreateTenantUserAsync(
        Guid tenantId,
        [FromBody] CreateTenantUserRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateTenantUserCommand(
            tenantId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.RoleIds);

        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/system/tenants/{tenantId}/users/{result.Value!.UrlCode}", result.Value)
            : MapError(result.FirstError);
    }

    private static IResult MapError(Error error) =>
        Results.Json(new { error.Code, error.Message }, statusCode: error.Type switch
        {
            ErrorType.Validation   => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden    => StatusCodes.Status403Forbidden,
            ErrorType.NotFound     => StatusCodes.Status404NotFound,
            ErrorType.Conflict     => StatusCodes.Status409Conflict,
            ErrorType.Failure      => StatusCodes.Status422UnprocessableEntity,
            _                      => StatusCodes.Status500InternalServerError,
        });
}

/// <summary>Tenant kullanıcısı oluşturma isteği.</summary>
public sealed record CreateTenantUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    IReadOnlyList<Guid> RoleIds);
