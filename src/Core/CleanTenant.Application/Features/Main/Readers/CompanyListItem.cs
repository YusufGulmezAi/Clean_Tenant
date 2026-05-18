using CleanTenant.Domain.Tenant.Companies;

namespace CleanTenant.Application.Features.Main.Readers;

/// <summary>
/// <para>
/// Company (Site) liste sayfası için projection DTO. Parent <see cref="TenantId"/>
/// + (opsiyonel) <see cref="TenantName"/> denormalize — Sistem operatörünün
/// "tüm siteler" sayfasında ekstra DB join'i önler.
/// </para>
/// </summary>
public sealed record CompanyListItem(
    Guid Id,
    Guid TenantId,
    string? TenantName,
    string UrlCode,
    string Name,
    string? LegalName,
    string? Vkn,
    CompanyStatus Status);
