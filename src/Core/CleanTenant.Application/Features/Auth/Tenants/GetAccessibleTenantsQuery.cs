using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.Tenants;

/// <summary>
/// <para>
/// Mevcut authenticated kullanıcının erişebileceği tenant'ları listeler.
/// AppBar'daki "Aktif Tenant" dropdown'unu doldurur (v0.2.3.b).
/// </para>
/// <para>
/// <b>Davranış:</b>
/// </para>
/// <list type="bullet">
///   <item>System scope kullanıcı (Developer / SystemAdmin vb.) → <b>tüm Active
///   tenant'lar</b> dönülür (cross-tenant erişim hakkı).</item>
///   <item>Tenant/Company/Unit scope kullanıcı → yalnız <b>rol ataması olan</b>
///   tenant'lar (UserRoleAssignments üzerinden distinct).</item>
/// </list>
/// </summary>
public sealed record GetAccessibleTenantsQuery : IRequest<Result<IReadOnlyList<AccessibleTenant>>>;

/// <summary>
/// Erişilebilir tenant öğesi. Dropdown'da görüntü + switch-tenant request'inde
/// kullanılacak id.
/// </summary>
/// <param name="TenantId">Tenant kimliği (switch-tenant'a gönderilir).</param>
/// <param name="UrlCode">9 karakter Base58 URL kodu (UI'da rozet için).</param>
/// <param name="Name">Görünür ad.</param>
public sealed record AccessibleTenant(Guid TenantId, string UrlCode, string Name);
