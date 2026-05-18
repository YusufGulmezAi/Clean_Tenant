using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.Tenants;

/// <summary>
/// <para>
/// AppBar ContextSwitcher dropdown'unu doldurur: kullanıcının erişebileceği
/// Tenant'ları + her Tenant altındaki Şirketleri (Company) hiyerarşik biçimde
/// listeler. v0.2.3.b'de eklenen <see cref="GetAccessibleTenantsQuery"/>'in
/// genişletilmiş hâli — Company bilgisi de gelir.
/// </para>
/// <para>
/// <b>Davranış:</b>
/// </para>
/// <list type="bullet">
///   <item>System scope kullanıcı → tüm Active tenant'lar + her birinin tüm
///   Companies'i (Main DB'den IgnoreQueryFilters ile okunur).</item>
///   <item>Tenant scope kullanıcı → yalnız rol ataması olan tenant'lar + o
///   tenant'ların Company'leri (global query filter zaten o tenant'ı filtreler).</item>
/// </list>
/// </summary>
public sealed record GetAccessibleContextsQuery : IRequest<Result<IReadOnlyList<AccessibleTenantContext>>>;

/// <summary>
/// Erişilebilir Tenant + altındaki Company'ler. ContextSwitcher hiyerarşik
/// gösterimde kullanır.
/// </summary>
/// <param name="TenantId">Tenant kimliği.</param>
/// <param name="UrlCode">Tenant 9 karakter Base58 URL kodu.</param>
/// <param name="Name">Tenant görünür adı.</param>
/// <param name="Companies">Bu tenant altındaki Şirketler (Active).</param>
public sealed record AccessibleTenantContext(
    Guid TenantId,
    string UrlCode,
    string Name,
    IReadOnlyList<AccessibleCompany> Companies);

/// <summary>Tenant altındaki bir Company girdisi.</summary>
/// <param name="CompanyId">Company kimliği.</param>
/// <param name="UrlCode">Company 9 karakter Base58 URL kodu.</param>
/// <param name="Name">Company görünür adı.</param>
public sealed record AccessibleCompany(
    Guid CompanyId,
    string UrlCode,
    string Name);
