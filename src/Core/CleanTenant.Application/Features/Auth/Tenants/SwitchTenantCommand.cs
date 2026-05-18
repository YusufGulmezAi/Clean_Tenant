using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.Tenants;

/// <summary>
/// <para>
/// Mevcut oturumda <b>aktif tenant</b>'ı değiştirme isteği — Companies / Audit
/// gibi tenant-scoped UI'ların hangi tenant bağlamında çalışacağını belirler.
/// </para>
/// <para>
/// <b>Davranış:</b>
/// </para>
/// <list type="bullet">
///   <item>System scope kullanıcı (Developer / SystemAdmin) → herhangi bir Active
///   tenant'a geçebilir; cross-tenant erişim hakkı vardır. Permissions ve roller
///   System scope'tan miras alınır (cross-tenant operasyonel görünürlük).</item>
///   <item>Alt scope kullanıcı → yalnız UserRoleAssignments'ında bulunan
///   tenant'a geçebilir; Permissions o tenant'taki rol atamasından gelir
///   (mevcut <c>SwitchContextCommand</c> ile aynı davranış).</item>
/// </list>
/// <para>
/// <b>Side-effect:</b> Yeni AuthSession Redis'e yazılır, eski session silinir,
/// refresh token chain revoke + yeni token üretilir, yeni JWT döner.
/// </para>
/// </summary>
/// <param name="TargetTenantId">Geçiş yapılacak tenant kimliği.</param>
/// <param name="IpAddress">İstemci IP'si (audit).</param>
/// <param name="UserAgent">İstemci User-Agent (audit).</param>
public sealed record SwitchTenantCommand(
    Guid TargetTenantId,
    string IpAddress,
    string UserAgent) : IRequest<Result<TokenPair>>;
