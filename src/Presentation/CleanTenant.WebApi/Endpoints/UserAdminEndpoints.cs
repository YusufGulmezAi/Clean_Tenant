using CleanTenant.Application.Features.System.ForceLogoutUser;
using CleanTenant.Application.Features.System.Users;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Admin endpoint'leri. v0.1.6'dan itibaren <see cref="IMediator"/> üzerinden.
/// </summary>
public static class UserAdminEndpoints
{
    /// <summary>User admin endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapUserAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/users").WithTags("UserAdmin");

        group.MapPost("/{userUrlCode}/force-logout", ForceLogoutUserAsync)
             .RequireAuthorization(AuthorizationPolicies.TenantScope);

        group.MapPost("/{userUrlCode}/unlock", UnlockUserAsync)
             .RequireAuthorization(AuthorizationPolicies.TenantScope);

        return routes;
    }

    private static async Task<IResult> UnlockUserAsync(
        string userUrlCode,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnlockUserCommand(userUrlCode), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { message = "Kullanıcının hesap kilidi açıldı." })
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> ForceLogoutUserAsync(
        string userUrlCode,
        [FromBody] ForceLogoutUserRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ForceLogoutUserCommand(userUrlCode, request.Reason);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { message = "Kullanıcının tüm session'ları sonlandırıldı." })
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

/// <summary>Force-logout isteği gövdesi.</summary>
public sealed record ForceLogoutUserRequest(string Reason);
