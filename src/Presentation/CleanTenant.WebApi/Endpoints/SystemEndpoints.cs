using CleanTenant.Application.Features.System.ElevateToWrite;
using CleanTenant.Application.Features.System.EnterSupportMode;
using CleanTenant.Application.Features.System.ExitSupportMode;
using CleanTenant.Application.Features.System.GetSystemSupportSessions;
using CleanTenant.Application.Features.System.ImpersonateUser;
using CleanTenant.Application.Features.System.RevokeSession;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// System operatörü endpoint'leri. v0.1.6'dan itibaren <see cref="IMediator"/>
/// üzerinden gönderilir.
/// </summary>
public static class SystemEndpoints
{
    /// <summary>System endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/system").WithTags("System");

        group.MapPost("/sessions/{sessionId:guid}/revoke", RevokeSessionAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPost("/support/enter", EnterSupportModeAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPost("/support/exit", ExitSupportModeAsync)
             .RequireAuthorization(AuthorizationPolicies.SupportModeActive);

        group.MapPost("/support/elevate", ElevateToWriteAsync)
             .RequireAuthorization(AuthorizationPolicies.SupportModeActive);

        group.MapPost("/support/impersonate", ImpersonateUserAsync)
             .RequireAuthorization(AuthorizationPolicies.SupportModeActive);

        group.MapGet("/support-sessions", GetSystemSupportSessionsAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        return routes;
    }

    private static async Task<IResult> RevokeSessionAsync(
        Guid sessionId,
        [FromBody] RevokeSessionRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RevokeSessionCommand(sessionId, request.Reason);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { message = "Session revoke edildi." })
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> EnterSupportModeAsync(
        [FromBody] EnterSupportModeRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new EnterSupportModeCommand(request.TargetTenantId, request.Reason, ip, ua);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> ExitSupportModeAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExitSupportModeCommand(), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> ElevateToWriteAsync(
        [FromBody] ElevateToWriteRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ElevateToWriteCommand(request.Reason);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { message = "Support Mode WriteEnabled'a yükseltildi." })
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> ImpersonateUserAsync(
        [FromBody] ImpersonateUserRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new ImpersonateUserCommand(request.TargetUserUrlCode, request.Reason, ip, ua);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> GetSystemSupportSessionsAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? operatorUserUrlCode = null,
        [FromQuery] string? targetTenantUrlCode = null,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetSystemSupportSessionsQuery(
            from, to, operatorUserUrlCode, targetTenantUrlCode, page, pageSize);
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

/// <summary>Revoke session isteği gövdesi.</summary>
public sealed record RevokeSessionRequest(string Reason);

/// <summary>Support Mode'a giriş isteği.</summary>
public sealed record EnterSupportModeRequest(Guid TargetTenantId, string Reason);

/// <summary>ReadOnly → WriteEnabled yükseltme isteği.</summary>
public sealed record ElevateToWriteRequest(string Reason);

/// <summary>Impersonate isteği.</summary>
public sealed record ImpersonateUserRequest(string TargetUserUrlCode, string Reason);
