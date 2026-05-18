using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Application.Features.Auth.Logout;
using CleanTenant.Application.Features.Auth.LogoutAllSessions;
using CleanTenant.Application.Features.Auth.Refresh;
using CleanTenant.Application.Features.Auth.SwitchContext;
using CleanTenant.Application.Features.Auth.Tenants;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// <c>/api/v1/auth/*</c> endpoint'leri. v0.1.6'dan itibaren handler doğrudan
/// inject edilmez — <see cref="IMediator"/> üzerinden gönderilir; pipeline
/// behavior'lar (Auth → Validation → Logging) handler'dan önce çalışır.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>Auth endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapPost("/switch-context", SwitchContextAsync).RequireAuthorization();

        // v0.2.3.b — AppBar "Aktif Tenant" dropdown akışı.
        group.MapGet("/accessible-tenants", GetAccessibleTenantsAsync).RequireAuthorization();
        group.MapPost("/switch-tenant", SwitchTenantAsync).RequireAuthorization();

        // Self: tüm cihazlardan çıkış
        routes.MapPost("/api/v1/users/me/sessions/logout-all", LogoutAllSessionsAsync)
              .WithTags("Auth")
              .RequireAuthorization();

        return routes;
    }

    private static async Task<IResult> SwitchContextAsync(
        [FromBody] SwitchContextRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new SwitchContextCommand(
            request.ScopeLevel,
            request.TenantId,
            request.CompanyId,
            request.UnitId,
            ip, ua);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> GetAccessibleTenantsAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAccessibleTenantsQuery(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> SwitchTenantAsync(
        [FromBody] SwitchTenantRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new SwitchTenantCommand(request.TenantId, ip, ua);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> LogoutAllSessionsAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LogoutAllSessionsCommand(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new { message = "Tüm cihazlardan çıkış yapıldı." })
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new LoginCommand(request.Identifier, request.Password, request.Persona, request.ContextId, ip, ua);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new RefreshTokenCommand(request.RefreshToken, ip, ua);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> LogoutAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LogoutCommand(), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { message = "Çıkış yapıldı." })
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static int MapErrorTypeToStatus(SharedKernel.Common.Errors.ErrorType type) => type switch
    {
        SharedKernel.Common.Errors.ErrorType.Validation => StatusCodes.Status400BadRequest,
        SharedKernel.Common.Errors.ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        SharedKernel.Common.Errors.ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        SharedKernel.Common.Errors.ErrorType.NotFound => StatusCodes.Status404NotFound,
        SharedKernel.Common.Errors.ErrorType.Conflict => StatusCodes.Status409Conflict,
        SharedKernel.Common.Errors.ErrorType.Failure => StatusCodes.Status422UnprocessableEntity,
        SharedKernel.Common.Errors.ErrorType.Critical => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest,
    };
}

/// <summary>Login isteği gövdesi.</summary>
/// <param name="Identifier">
/// Email, TCKN / YKN (11 hane), VKN (10 hane) veya cep telefonu numarası.
/// </param>
/// <param name="Password">Düz metin şifre.</param>
/// <param name="Persona">Login tarafı.</param>
/// <param name="ContextId">Sekme/persona context kimliği; null ise sunucu üretir.</param>
public sealed record LoginRequest(
    string Identifier,
    string Password,
    PersonaSide Persona,
    Guid? ContextId);

/// <summary>Refresh isteği gövdesi.</summary>
/// <param name="RefreshToken">İstemcide saklanan raw refresh token.</param>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Switch-context isteği gövdesi.</summary>
/// <param name="ScopeLevel">Hedef scope seviyesi.</param>
/// <param name="TenantId">Tenant kapsam (Tenant/Company/Unit'te dolu).</param>
/// <param name="CompanyId">Company kapsam (Company'de dolu).</param>
/// <param name="UnitId">Unit kapsam (Unit'te dolu).</param>
public sealed record SwitchContextRequest(
    ScopeLevel ScopeLevel,
    Guid? TenantId,
    Guid? CompanyId,
    Guid? UnitId);

/// <summary>v0.2.3.b — Switch-tenant isteği gövdesi (AppBar dropdown).</summary>
/// <param name="TenantId">Geçilecek tenant kimliği.</param>
public sealed record SwitchTenantRequest(Guid TenantId);
