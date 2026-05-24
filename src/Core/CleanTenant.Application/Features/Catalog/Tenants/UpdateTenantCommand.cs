using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <para>
/// Mevcut Yönetim'in bilgilerini günceller. Yetki davranışı:
/// </para>
/// <list type="bullet">
///   <item><b>Sistem scope</b> → tüm alanları değiştirebilir (kimlik, BillingTier, dedicated DB dahil).</item>
///   <item><b>TenantAdmin (kendi yönetimi)</b> → yalnız Name, LegalName, Address, AllowSystemWriteAccess değişebilir.
///   Kimlik bilgisi, BillingTier, dedicated DB değişimi reddedilir.</item>
/// </list>
/// <para>
/// <c>RequirePermission</c> attribute kullanılmaz çünkü iki kullanım kaynağı var
/// (Sistem operatör için <c>Tenant.Update</c>, TenantAdmin için kendi yetkisi).
/// Yetki handler içinde session scope üzerinden kontrol edilir.
/// </para>
/// </summary>
[TenantWriteOperation]
public sealed record UpdateTenantCommand(
    Guid TenantId,
    string Name,
    string? LegalName,
    LegalIdentityType LegalIdentityType,
    string LegalIdentityNumber,
    string? Address,
    BillingTier BillingTier,
    bool HasDedicatedDatabase,
    bool AllowSystemWriteAccess,
    Guid? ProvinceId = null,
    Guid? DistrictId = null,
    Guid? NeighborhoodId = null,
    string? ContactPerson = null,
    string? ContactEmail = null,
    string? ContactPhone = null,
    DateOnly? ContractStartDate = null,
    DateOnly? ContractEndDate = null,
    int? TransitionGraceDays = null,
    TenantStatus Status = TenantStatus.Active,
    bool LockoutEnabled = true,
    int LockoutMaxFailedAttempts = 5,
    int LockoutDurationMinutes = 15) : IRequest<Result>;
