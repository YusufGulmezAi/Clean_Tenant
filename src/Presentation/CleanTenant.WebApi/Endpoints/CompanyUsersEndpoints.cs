using CleanTenant.Application.Features.CompanyUsers;
using CleanTenant.Application.Features.System.Users;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Belirli bir Site'ye (Company) ait kullanıcı yönetim endpoint'leri.
/// Sistem operatörü tarafından /system/tenants/{tenantId}/companies/{companyId}/users üzerinden erişilir.
/// </summary>
public static class CompanyUsersEndpoints
{
    /// <summary>Endpoint'leri route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapCompanyUsersEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/v1/system/tenants/{tenantId}/companies/{companyId}/users")
            .WithTags("Company.Users");

        group.MapGet("/", ListCompanyUsersAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        group.MapPost("/", CreateCompanyUserAsync)
             .RequireAuthorization(AuthorizationPolicies.SystemScope);

        return routes;
    }

    private static async Task<IResult> ListCompanyUsersAsync(
        Guid tenantId,
        Guid companyId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null)
    {
        var query = new ListUsersQuery(ScopeLevel.Company, TenantId: tenantId, CompanyId: companyId, Search: search);
        var result = await mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> CreateCompanyUserAsync(
        Guid tenantId,
        Guid companyId,
        [FromBody] CreateCompanyUserRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateCompanyUserCommand(
            tenantId,
            companyId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.RoleIds);

        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/system/tenants/{tenantId}/companies/{companyId}/users/{result.Value!.UrlCode}",
                result.Value)
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

/// <summary>Site (company) kullanıcısı oluşturma isteği.</summary>
public sealed record CreateCompanyUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    IReadOnlyList<Guid> RoleIds);
