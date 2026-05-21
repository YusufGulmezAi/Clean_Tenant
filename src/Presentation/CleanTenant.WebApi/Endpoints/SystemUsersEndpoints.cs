using CleanTenant.Application.Features.System.Users;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Sistem kullanıcı yönetim endpoint'leri (v0.2.12).
/// İzin kontrolü Application katmanında <c>[RequirePermission]</c> ile yapılır;
/// endpoint yalnız System scope policy'sini zorunlu kılar.
/// </summary>
public static class SystemUsersEndpoints
{
    /// <summary>Endpoint'leri route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapSystemUsersEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/v1/system/users")
            .WithTags("System.Users");

        group.MapGet("/", ListUsersAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapGet("/{urlCode}", GetUserDetailAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapGet("/roles", GetRoleOptionsAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPost("/", CreateUserAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPut("/{urlCode}", UpdateUserAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapDelete("/{urlCode}", DeactivateUserAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPost("/{urlCode}/reset-password", ResetPasswordAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPost("/{urlCode}/reactivate", ReactivateUserAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        return routes;
    }

    private static async Task<IResult> ListUsersAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null)
    {
        var query = new ListUsersQuery(ScopeLevel.System, Search: search);
        var result = await mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> GetUserDetailAsync(
        string urlCode,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetUserDetailQuery(urlCode, ScopeLevel.System);
        var result = await mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> GetRoleOptionsAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetRoleOptionsQuery(ScopeLevel.System);
        var result = await mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> CreateUserAsync(
        [FromBody] CreateSystemUserRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateSystemUserCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.RoleIds);

        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/system/users/{result.Value!.UrlCode}", result.Value)
            : MapError(result.FirstError);
    }

    private static async Task<IResult> UpdateUserAsync(
        string urlCode,
        [FromBody] UpdateUserRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(
            urlCode,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            ScopeLevel.System,
            TenantId: null,
            CompanyId: null,
            request.RoleIds);

        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> DeactivateUserAsync(
        string urlCode,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateUserCommand(urlCode);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : MapError(result.FirstError);
    }

    private static async Task<IResult> ResetPasswordAsync(
        string urlCode,
        [FromBody] ResetPasswordRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ResetUserPasswordCommand(urlCode, request.NewPassword);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : MapError(result.FirstError);
    }

    private static async Task<IResult> ReactivateUserAsync(
        string urlCode,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ReactivateUserCommand(urlCode);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : MapError(result.FirstError);
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

/// <summary>Sistem kullanıcısı oluşturma isteği.</summary>
public sealed record CreateSystemUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    IReadOnlyList<Guid> RoleIds);

/// <summary>Kullanıcı güncelleme isteği.</summary>
public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    IReadOnlyList<Guid> RoleIds);

/// <summary>Şifre sıfırlama isteği.</summary>
public sealed record ResetPasswordRequest(string NewPassword);
