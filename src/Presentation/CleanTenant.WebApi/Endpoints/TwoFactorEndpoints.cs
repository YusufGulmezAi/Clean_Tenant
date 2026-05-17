using CleanTenant.Application.Features.Auth.TwoFactor.ConfirmTotpEnrollment;
using CleanTenant.Application.Features.Auth.TwoFactor.DisableTotp;
using CleanTenant.Application.Features.Auth.TwoFactor.EnrollTotp;
using CleanTenant.Application.Features.Auth.TwoFactor.GetTwoFactorMethods;
using CleanTenant.Application.Features.Auth.TwoFactor.RegenerateRecoveryCodes;
using CleanTenant.Application.Features.Auth.TwoFactor.SendCode;
using CleanTenant.Application.Features.Auth.TwoFactor.VerifyTwoFactor;
using CleanTenant.SharedKernel.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// v0.1.5.c — 2FA endpoint'leri. Login akışı challenge dönerse istemci
/// <c>verify</c> veya <c>send-code</c>'a gider. Bearer ile authenticate
/// kullanıcılar enrollment / disable / recovery üretimi yapabilir.
/// </summary>
public static class TwoFactorEndpoints
{
    /// <summary>2FA endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapTwoFactorEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/auth/2fa").WithTags("TwoFactor");

        // Login akışı sırasındaki anonim çağrılar (challenge token ile yetkili).
        group.MapPost("/verify", VerifyAsync).AllowAnonymous();
        group.MapPost("/send-code", SendCodeAsync).AllowAnonymous();

        // Authenticated kullanıcı çağrıları (kendi hesabı için).
        group.MapPost("/enroll/totp", EnrollTotpAsync).RequireAuthorization();
        group.MapPost("/enroll/totp/confirm", ConfirmTotpAsync).RequireAuthorization();
        group.MapPost("/disable/totp", DisableTotpAsync).RequireAuthorization();
        group.MapPost("/recovery-codes/regenerate", RegenerateRecoveryAsync).RequireAuthorization();
        group.MapGet("/methods", GetMethodsAsync).RequireAuthorization();

        return routes;
    }

    private static async Task<IResult> VerifyAsync(
        [FromBody] VerifyTwoFactorRequest request,
        [FromServices] VerifyTwoFactorCommandHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new VerifyTwoFactorCommand(request.ChallengeToken, request.Method, request.Code, ip, ua);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> SendCodeAsync(
        [FromBody] SendTwoFactorCodeRequest request,
        [FromServices] SendTwoFactorCodeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new SendTwoFactorCodeCommand(request.ChallengeToken, request.Method);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { message = "Kod gönderildi." })
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> EnrollTotpAsync(
        [FromServices] EnrollTotpCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new EnrollTotpCommand(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> ConfirmTotpAsync(
        [FromBody] ConfirmTotpRequest request,
        [FromServices] ConfirmTotpEnrollmentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ConfirmTotpEnrollmentCommand(request.Code), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> DisableTotpAsync(
        [FromServices] DisableTotpCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DisableTotpCommand(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new { message = "TOTP kapatıldı." })
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> RegenerateRecoveryAsync(
        [FromServices] RegenerateRecoveryCodesCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RegenerateRecoveryCodesCommand(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> GetMethodsAsync(
        [FromServices] GetTwoFactorMethodsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTwoFactorMethodsQuery(), cancellationToken);
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
/// <param name="ChallengeToken">Login response'tan alınan token.</param>
/// <param name="Method"><c>"Authenticator"</c> / <c>"Email"</c> / <c>"Phone"</c> / <c>"RecoveryCode"</c>.</param>
/// <param name="Code">Kullanıcının girdiği kod.</param>
public sealed record VerifyTwoFactorRequest(Guid ChallengeToken, string Method, string Code);

/// <summary>2FA send-code isteği gövdesi (yalnız Email/Phone).</summary>
/// <param name="ChallengeToken">Login response'tan alınan token.</param>
/// <param name="Method"><c>"Email"</c> veya <c>"Phone"</c>.</param>
public sealed record SendTwoFactorCodeRequest(Guid ChallengeToken, string Method);

/// <summary>TOTP confirm isteği gövdesi.</summary>
/// <param name="Code">Authenticator app'in ürettiği ilk doğrulama kodu.</param>
public sealed record ConfirmTotpRequest(string Code);
