using CleanTenant.Application.Features.Auth.TwoFactor.ConfirmTotpEnrollment;
using CleanTenant.Application.Features.Auth.TwoFactor.DisableTotp;
using CleanTenant.Application.Features.Auth.TwoFactor.EnrollTotp;
using CleanTenant.Application.Features.Auth.TwoFactor.GetTwoFactorMethods;
using CleanTenant.Application.Features.Auth.TwoFactor.PreAuthEnrollment;
using CleanTenant.Application.Features.Auth.TwoFactor.RegenerateRecoveryCodes;
using CleanTenant.Application.Features.Auth.TwoFactor.SendCode;
using CleanTenant.Application.Features.Auth.TwoFactor.VerifyTwoFactor;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// v0.1.5.c — 2FA endpoint'leri. v0.1.6'dan itibaren <see cref="IMediator"/>
/// üzerinden gönderilir; pipeline behavior'lar zincirini takip eder.
/// </summary>
public static class TwoFactorEndpoints
{
    /// <summary>2FA endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapTwoFactorEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/auth/2fa").WithTags("TwoFactor");

        group.MapPost("/verify", VerifyAsync).AllowAnonymous();
        group.MapPost("/send-code", SendCodeAsync).AllowAnonymous();

        // v0.2.2.a — Pre-auth enrollment (System scope kullanıcı + 2FA yok).
        // Kullanıcı henüz authenticated değil; tek başına challenge token taşır.
        group.MapPost("/enroll-pre-auth/start", PreAuthStartAsync).AllowAnonymous();
        group.MapPost("/enroll-pre-auth/complete", PreAuthCompleteAsync).AllowAnonymous();
        group.MapPost("/enroll-pre-auth/finalize", PreAuthFinalizeAsync).AllowAnonymous();

        group.MapPost("/enroll/totp", EnrollTotpAsync).RequireAuthorization();
        group.MapPost("/enroll/totp/confirm", ConfirmTotpAsync).RequireAuthorization();
        group.MapPost("/disable/totp", DisableTotpAsync).RequireAuthorization();
        group.MapPost("/recovery-codes/regenerate", RegenerateRecoveryAsync).RequireAuthorization();
        group.MapGet("/methods", GetMethodsAsync).RequireAuthorization();

        return routes;
    }

    private static async Task<IResult> VerifyAsync(
        [FromBody] VerifyTwoFactorRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new VerifyTwoFactorCommand(request.ChallengeToken, request.Method, request.Code, ip, ua);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> SendCodeAsync(
        [FromBody] SendTwoFactorCodeRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new SendTwoFactorCodeCommand(request.ChallengeToken, request.Method);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { message = "Kod gönderildi." })
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> PreAuthStartAsync(
        [FromBody] PreAuthStartRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new StartPreAuthEnrollmentQuery(request.ChallengeToken), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> PreAuthCompleteAsync(
        [FromBody] PreAuthCompleteRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CompletePreAuthEnrollmentCommand(request.ChallengeToken, request.Code), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> PreAuthFinalizeAsync(
        [FromBody] PreAuthFinalizeRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var result = await mediator.Send(
            new FinalizePreAuthEnrollmentCommand(request.ChallengeToken, ip, ua), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> EnrollTotpAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new EnrollTotpCommand(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> ConfirmTotpAsync(
        [FromBody] ConfirmTotpRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ConfirmTotpEnrollmentCommand(request.Code), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> DisableTotpAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DisableTotpCommand(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new { message = "TOTP kapatıldı." })
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> RegenerateRecoveryAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RegenerateRecoveryCodesCommand(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> GetMethodsAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTwoFactorMethodsQuery(), cancellationToken);
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

/// <summary>2FA verify isteği gövdesi (login challenge için).</summary>
public sealed record VerifyTwoFactorRequest(Guid ChallengeToken, string Method, string Code);

/// <summary>2FA send-code isteği gövdesi (yalnız Email/Phone).</summary>
public sealed record SendTwoFactorCodeRequest(Guid ChallengeToken, string Method);

/// <summary>TOTP confirm isteği gövdesi.</summary>
public sealed record ConfirmTotpRequest(string Code);

/// <summary>Pre-auth enrollment start isteği gövdesi (v0.2.2.a).</summary>
public sealed record PreAuthStartRequest(Guid ChallengeToken);

/// <summary>Pre-auth enrollment complete isteği gövdesi (v0.2.2.a).</summary>
public sealed record PreAuthCompleteRequest(Guid ChallengeToken, string Code);

/// <summary>Pre-auth enrollment finalize isteği gövdesi (v0.2.2.a).</summary>
public sealed record PreAuthFinalizeRequest(Guid ChallengeToken);
