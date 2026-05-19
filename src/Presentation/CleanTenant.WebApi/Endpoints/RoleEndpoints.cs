using CleanTenant.Application.Features.Catalog.Permissions;
using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.Application.Features.Catalog.Roles;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Role ve Permission yönetimi endpoint'leri.
/// </summary>
public static class RoleEndpoints
{
    /// <summary>Role endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/roles").WithTags("Roles");
        var permissionGroup = routes.MapGroup("/api/v1/permissions").WithTags("Permissions");

        // Roles
        group.MapGet(string.Empty, GetRolesByScopeAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPost(string.Empty, CreateRoleAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapGet("{id:guid}", GetRoleDetailAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPut("{id:guid}", UpdateRoleAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapDelete("{id:guid}", DeleteRoleAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPut("{id:guid}/permissions", AssignPermissionsAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        // Permissions
        permissionGroup.MapGet(string.Empty, GetPermissionsAsync)
                       .RequireAuthorization(AuthorizationPolicies.SystemScope);

        return routes;
    }

    private static async Task<IResult> GetRolesByScopeAsync(
        [FromServices] IAuthorizationCatalogReader reader,
        [FromQuery] int scopeLevel,
        CancellationToken cancellationToken)
    {
        var roles = await reader.GetRolesByScopeAsync(scopeLevel, cancellationToken);
        return Results.Ok(roles);
    }

    private static async Task<IResult> CreateRoleAsync(
        [FromServices] IMediator mediator,
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(request.Name, request.Scope, request.Description);
        var result = await mediator.Send(command, cancellationToken);

        return Results.CreatedAtRoute(nameof(GetRoleDetailAsync), new { id = result }, result);
    }

    private static async Task<IResult> GetRoleDetailAsync(
        [FromServices] IAuthorizationCatalogReader reader,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var role = await reader.GetRoleDetailAsync(id, cancellationToken);

        return role is not null
            ? Results.Ok(role)
            : Results.NotFound();
    }

    private static async Task<IResult> UpdateRoleAsync(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoleCommand(id, request.Name, request.Description);

        try
        {
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteRoleAsync(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRoleCommand(id);

        try
        {
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> AssignPermissionsAsync(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        [FromBody] AssignPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignPermissionsToRoleCommand(id, request.PermissionIds);

        try
        {
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetPermissionsAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetPermissionsQuery();
        var permissions = await mediator.Send(query, cancellationToken);
        return Results.Ok(permissions);
    }
}

/// <summary>Role oluşturma request'i.</summary>
public sealed record CreateRoleRequest(
    string Name,
    int Scope,
    string? Description);

/// <summary>Role güncelleme request'i.</summary>
public sealed record UpdateRoleRequest(
    string Name,
    string? Description);

/// <summary>Permission atama request'i.</summary>
public sealed record AssignPermissionsRequest(
    IReadOnlyList<Guid> PermissionIds);
