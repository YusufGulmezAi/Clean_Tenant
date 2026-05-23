using CleanTenant.Application.Common.Export;
using CleanTenant.Application.Features.Main.Companies;
using CleanTenant.Application.Features.Main.Readers;
using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.Infrastructure.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Company (Site) yönetim endpoint'leri.
/// </summary>
public static class CompanyEndpoints
{
    /// <summary>Company endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder routes)
    {
        // Sistem operatörü: tüm şirketler cross-tenant
        routes.MapGet("/api/v1/companies", GetAllCompaniesAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemScope)
            .WithName("GetAllCompanies");

        // Tenant operatörü: tenant'a özel şirketler
        routes.MapGet("/api/v1/tenants/{tenantId:guid}/companies", GetCompaniesForTenantAsync)
            .RequireAuthorization(AuthorizationPolicies.TenantScope)
            .WithName("GetCompaniesForTenant");

        // Yeni şirket oluştur
        routes.MapPost("/api/v1/tenants/{tenantId:guid}/companies", CreateCompanyAsync)
            .RequireAuthorization(AuthorizationPolicies.TenantScope)
            .WithName("CreateCompany");

        // Şirket detayı (edit form)
        routes.MapGet("/api/v1/companies/{companyId:guid}", GetCompanyDetailAsync)
            .RequireAuthorization(AuthorizationPolicies.TenantScope)
            .WithName("GetCompanyDetail");

        // Şirket güncelle
        routes.MapPut("/api/v1/companies/{companyId:guid}", UpdateCompanyAsync)
            .RequireAuthorization(AuthorizationPolicies.TenantScope)
            .WithName("UpdateCompany");

        // Şirket sil
        routes.MapDelete("/api/v1/companies/{companyId:guid}", DeleteCompanyAsync)
            .RequireAuthorization(AuthorizationPolicies.TenantScope)
            .WithName("DeleteCompany");

        return routes;
    }

    private static async Task<IResult> GetAllCompaniesAsync(
        [FromServices] IMainCatalogReader reader,
        CancellationToken cancellationToken)
    {
        var companies = await reader.GetAllGlobalAsync(cancellationToken);
        return Results.Ok(companies);
    }

    private static async Task<IResult> GetCompaniesForTenantAsync(
        Guid tenantId,
        [FromServices] IMainCatalogReader reader,
        CancellationToken cancellationToken)
    {
        var companies = await reader.GetByTenantAsync(tenantId, cancellationToken);
        return Results.Ok(companies);
    }

    private static async Task<IResult> CreateCompanyAsync(
        Guid tenantId,
        [FromBody] CreateCompanyRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateCompanyCommand(
            tenantId, request.Name, request.LegalName, request.Vkn, request.Email, request.Phone,
            request.AdminFirstName, request.AdminLastName, request.AdminEmail, request.AdminPhone);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetCompanyDetail", new { companyId = result.Value!.Id }, result.Value!)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> GetCompanyDetailAsync(
        Guid companyId,
        [FromServices] IMainCatalogReader reader,
        CancellationToken cancellationToken)
    {
        var company = await reader.GetDetailByIdAsync(companyId, cancellationToken);
        return company is not null
            ? Results.Ok(company)
            : Results.NotFound();
    }

    private static async Task<IResult> UpdateCompanyAsync(
        Guid companyId,
        [FromBody] UpdateCompanyRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCompanyCommand(companyId, request.Name, request.LegalName, request.Vkn, request.Email, request.Phone, request.Status);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(new { errors = result.Errors }, statusCode: MapErrorTypeToStatus(result.FirstError.Type));
    }

    private static async Task<IResult> DeleteCompanyAsync(
        Guid companyId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCompanyCommand(companyId);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
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

/// <summary>Company oluşturma isteği. v0.2.13.e — zorunlu CompanyAdmin alanları.</summary>
public record CreateCompanyRequest(
    string Name,
    string? LegalName,
    string? Vkn,
    string? Email,
    string? Phone,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string? AdminPhone);

/// <summary>Company güncelleme isteği.</summary>
public record UpdateCompanyRequest(
    string Name,
    string? LegalName,
    string? Vkn,
    string? Email,
    string? Phone,
    CompanyStatus Status = CompanyStatus.Active);
