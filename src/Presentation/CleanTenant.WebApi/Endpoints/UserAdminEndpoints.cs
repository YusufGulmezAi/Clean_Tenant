using CleanTenant.Application.Features.System.ForceLogoutUser;
using CleanTenant.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;
using CleanTenant.SharedKernel.Common.Errors;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Admin endpoint'leri — kullanıcı yönetimi (v0.1.5.b.1: yalnız force-logout).
/// </summary>
public static class UserAdminEndpoints
{
    /// <summary>User admin endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapUserAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/users").WithTags("UserAdmin");

        // Tenant Admin veya System operator: hedef kullanıcının tüm session'larını sonlandır.
        // v0.1.5.b.1'de policy = TenantScope OR SystemScope (System için ayrı endpoint var).
        // Pragmatik: TenantScope policy yeterli (System scope kontrolü ayrıca handler içinde).
        group.MapPost("/{userUrlCode}/force-logout", ForceLogoutUserAsync)
             .RequireAuthorization(AuthorizationPolicies.TenantScope);

        return routes;
    }

    private static async Task<IResult> ForceLogoutUserAsync(
        string userUrlCode,
        [FromBody] ForceLogoutUserRequest request,
        [FromServices] ForceLogoutUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ForceLogoutUserCommand(userUrlCode, request.Reason);
        var result = await handler.HandleAsync(command, cancellationToken);

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
/// <param name="Reason">Zorunlu sebep (min 20 karakter).</param>
public sealed record ForceLogoutUserRequest(string Reason);
