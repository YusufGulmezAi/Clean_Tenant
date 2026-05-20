using CleanTenant.Domain.Identity.Tenants;

namespace CleanTenant.Application.Features.Catalog.Readers;

/// <summary>
/// <para>
/// Yönetim düzenleme formu / ayrıntı görüntüsü için tüm alanları taşıyan DTO.
/// <see cref="TenantListItem"/>'a göre daha geniş; cache TTL daha uzun
/// (DetailMediumLived 10 dk).
/// </para>
/// </summary>
public sealed record TenantDetail(
    Guid Id,
    string UrlCode,
    string Name,
    string? LegalName,
    LegalIdentityType LegalIdentityType,
    string LegalIdentityNumber,
    string? Address,
    Guid? ProvinceId,
    string? ProvinceName,
    Guid? DistrictId,
    string? DistrictName,
    Guid? NeighborhoodId,
    string? NeighborhoodName,
    string? ContactPerson,
    string? ContactEmail,
    string? ContactPhone,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    int? TransitionGraceDays,
    TenantStatus Status,
    BillingTier BillingTier,
    bool HasDedicatedDatabase,
    string? DatabaseSchemaName,
    bool AllowSystemWriteAccess);
