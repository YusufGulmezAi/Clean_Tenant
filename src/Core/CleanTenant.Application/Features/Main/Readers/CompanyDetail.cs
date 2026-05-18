using CleanTenant.Domain.Tenant.Companies;

namespace CleanTenant.Application.Features.Main.Readers;

/// <summary>
/// Company (Site) edit formu için detay DTO. Tüm düzenlenebilir alanları içerir.
/// </summary>
public sealed record CompanyDetail(
    Guid Id,
    Guid TenantId,
    string UrlCode,
    string Name,
    string? LegalName,
    string? Vkn,
    string? Email,
    string? Phone,
    CompanyStatus Status);
