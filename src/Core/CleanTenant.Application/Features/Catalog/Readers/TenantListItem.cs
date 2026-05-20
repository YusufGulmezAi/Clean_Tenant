using CleanTenant.Domain.Identity.Tenants;

namespace CleanTenant.Application.Features.Catalog.Readers;

/// <summary>
/// <para>
/// Tenant (Yönetim) liste sayfası için projection DTO. Cache-friendly: entity
/// değil, sadece UI'nın gösterdiği alanlar. Entity'yi cache'lemek EF tracking,
/// proxy/lazy-load sorunları yaratır; DTO net + serialization güvenli.
/// </para>
/// </summary>
public sealed record TenantListItem(
    Guid Id,
    string UrlCode,
    string Name,
    string? LegalName,
    LegalIdentityType LegalIdentityType,
    string LegalIdentityNumber,
    TenantStatus Status,
    BillingTier BillingTier,
    bool AllowSystemWriteAccess,
    string? ProvinceName,
    string? DistrictName,
    string? NeighborhoodName,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    int? TransitionGraceDays);
