using CleanTenant.Application.Features.Main.Readers;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// Mevcut bir Site'nin bilgilerini güncelle.
/// </summary>
public sealed record UpdateCompanyCommand(
    Guid CompanyId,
    string Name,
    string? LegalName,
    string? Vkn,
    string? Email,
    string? Phone) : IRequest<Result<CompanyDetail>>;
