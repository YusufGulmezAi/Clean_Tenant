using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.Tenants;

/// <summary>
/// <para>
/// Mevcut oturumdaki aktif tenant'tan System scope'a geri dönüş isteği.
/// AppBar TenantSwitcher dropdown'unda "System Scope" seçeneğine tıklayan
/// kullanıcı için. Yalnız <b>System scope rol ataması olan kullanıcılar</b>
/// (Developer / SystemAdmin vb.) bu komutu kullanabilir; aksi takdirde
/// <c>AUTH-013</c> hata döner.
/// </para>
/// <para>
/// Side-effect: yeni AuthSession Redis'e yazılır (ScopeLevel=System, TenantId=null),
/// eski session silinir, refresh chain "RevertToSystem" reason'ıyla revoke + yeni
/// token üretilir, JWT yenilenir.
/// </para>
/// </summary>
/// <param name="IpAddress">İstemci IP'si (audit).</param>
/// <param name="UserAgent">İstemci User-Agent (audit).</param>
public sealed record SwitchToSystemCommand(
    string IpAddress,
    string UserAgent) : IRequest<Result<TokenPair>>;
