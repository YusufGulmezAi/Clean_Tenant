using CleanTenant.Application.Features.System.Localization;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// System operatörü için lokalizasyon yönetim endpoint'leri (v0.2.10.g).
/// Permission check Application katmanında <c>[RequirePermission]</c> attribute'u
/// ile yapılır; endpoint yalnız System scope policy'sini zorunlu kılar.
/// </summary>
public static class SystemLocalizationEndpoints
{
    /// <summary>Lokalizasyon yönetim endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapSystemLocalizationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/v1/system/localization")
            .WithTags("System.Localization");

        group.MapGet("/entries", GetEntriesAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPut("/entries", UpdateEntryAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        return routes;
    }

    private static async Task<IResult> GetEntriesAsync(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] string culture = "tr-TR",
        [FromQuery] string? search = null,
        [FromQuery] bool onlyMachineTranslated = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50)
    {
        var filter = new LocalizationEntryFilter(culture, search, onlyMachineTranslated, page, pageSize);
        var query = new GetLocalizationEntriesQuery(filter);
        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateEntryAsync(
        [FromBody] UpdateLocalizationEntryRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLocalizationEntryCommand(request.Key, request.Culture, request.NewValue);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { message = "Lokalizasyon kaydı güncellendi." })
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

/// <summary>Lokalizasyon kaydı güncelleme isteği gövdesi.</summary>
public sealed record UpdateLocalizationEntryRequest(string Key, string Culture, string NewValue);
