using CleanTenant.Application.Features.Main.Readers;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// Belirli bir Yönetim (tenant) bağlamı içinde yeni Site oluştur.
/// UrlCode otomatik üretilir (Base58, 9 karakter).
/// </summary>
public sealed record CreateCompanyCommand(
    Guid TenantId,
    string Name,
    string? LegalName,
    string? Vkn,
    string? Email,
    string? Phone) : IRequest<Result<CompanyDetail>>;
