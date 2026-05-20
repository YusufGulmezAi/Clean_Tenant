using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <para>
/// Yeni Yönetim oluşturma komutu. Sistem operatör tarafından çağrılır;
/// Yönetim entity'si + ilk YönetimAdmin User + UserRoleAssignment(TenantAdmin/Tenant scope)
/// atomik bir Catalog transaction'ında yaratılır.
/// </para>
/// <para>
/// <b>Side-effect</b>:
/// </para>
/// <list type="bullet">
///   <item>Sorumlu Yöneticiye password reset token üretilir.</item>
///   <item>Welcome email gönderilir (reset link içerir; ilk şifre belirleme).</item>
///   <item>Cache invalidate (tüm tenant listesi yenilenir).</item>
/// </list>
/// </summary>
/// <param name="Name">Yönetim adı (tekil, max 256).</param>
/// <param name="LegalName">Yasal ad (opsiyonel, max 512).</param>
/// <param name="LegalIdentityType">Kimlik tipi (VKN/TCKN/YKN).</param>
/// <param name="LegalIdentityNumber">Kimlik numarası (tipe göre format).</param>
/// <param name="Address">Adres (opsiyonel, max 512).</param>
/// <param name="BillingTier">Faturalama katmanı.</param>
/// <param name="HasDedicatedDatabase">Dedicated DB kullanılacak mı.</param>
/// <param name="AdminFirstName">Sorumlu Yönetici adı.</param>
/// <param name="AdminLastName">Sorumlu Yönetici soyadı.</param>
/// <param name="AdminEmail">Sorumlu Yönetici e-postası (tekil).</param>
/// <param name="AdminPhone">Sorumlu Yönetici telefonu — format <c>0(5XX) XXX-XX-XX</c>.</param>
/// <param name="ProvinceId">Adres: bağlı il (LookUp.Provinces FK, opsiyonel). v0.2.11.b.</param>
/// <param name="DistrictId">Adres: bağlı ilçe (LookUp.Districts FK, opsiyonel).</param>
/// <param name="NeighborhoodId">Adres: bağlı mahalle (LookUp.Neighborhoods FK, opsiyonel).</param>
/// <param name="ContactPerson">İletişim kişisi adı-soyadı (opsiyonel, max 200).</param>
/// <param name="ContactEmail">İletişim e-postası (opsiyonel, max 256).</param>
/// <param name="ContactPhone">İletişim telefonu (opsiyonel, max 32).</param>
/// <param name="ContractStartDate">Sözleşme başlangıç tarihi (opsiyonel, gün hassasiyetli).</param>
/// <param name="ContractEndDate">Sözleşme bitiş tarihi (opsiyonel).</param>
/// <param name="TransitionGraceDays">Devir için verilen ek süre (gün, opsiyonel).</param>
[RequirePermission("Tenant.Create")]
[TenantWriteOperation]
public sealed record CreateTenantCommand(
    string Name,
    string? LegalName,
    LegalIdentityType LegalIdentityType,
    string LegalIdentityNumber,
    string? Address,
    BillingTier BillingTier,
    bool HasDedicatedDatabase,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPhone,
    Guid? ProvinceId = null,
    Guid? DistrictId = null,
    Guid? NeighborhoodId = null,
    string? ContactPerson = null,
    string? ContactEmail = null,
    string? ContactPhone = null,
    DateOnly? ContractStartDate = null,
    DateOnly? ContractEndDate = null,
    int? TransitionGraceDays = null) : IRequest<Result<CreateTenantResult>>;

/// <summary>
/// <see cref="CreateTenantCommand"/> sonucu. UI yeni Yönetim'in kart sayfasına
/// yönlendirebilmek için Tenant + Admin User id/email bilgisini taşır.
/// </summary>
public sealed record CreateTenantResult(
    Guid TenantId,
    string TenantUrlCode,
    Guid AdminUserId,
    string AdminEmail);
