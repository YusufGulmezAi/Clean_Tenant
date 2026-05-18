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
    string AdminPhone) : IRequest<Result<CreateTenantResult>>;

/// <summary>
/// <see cref="CreateTenantCommand"/> sonucu. UI yeni Yönetim'in kart sayfasına
/// yönlendirebilmek için Tenant + Admin User id/email bilgisini taşır.
/// </summary>
public sealed record CreateTenantResult(
    Guid TenantId,
    string TenantUrlCode,
    Guid AdminUserId,
    string AdminEmail);
