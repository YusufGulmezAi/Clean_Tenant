using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;

namespace CleanTenant.Application.Features.Auth.SwitchContext;

/// <summary>
/// <para>
/// Mevcut oturumda yetki kapsamını (scope) değiştirme isteği. Kullanıcı birden
/// çok scope'a atanmışsa istemci scope seçici sunar ve seçilen scope'la bu
/// endpoint'i çağırır.
/// </para>
/// <para>
/// <b>Persona değişimi yapılmaz</b> — sadece mevcut persona içinde scope geçişi.
/// (Persona değişimi için logout + yeni persona ile login gerekir.)
/// </para>
/// </summary>
/// <param name="TargetLevel">Hedef scope seviyesi.</param>
/// <param name="TargetTenantId">Hedef tenant id (Tenant/Company seviyelerinde dolu).</param>
/// <param name="TargetCompanyId">Hedef company id (Company seviyesinde dolu).</param>
/// <param name="TargetUnitId">Hedef unit id (Unit seviyesinde dolu).</param>
/// <param name="IpAddress">İstemci IP'si.</param>
/// <param name="UserAgent">İstemci User-Agent.</param>
public sealed record SwitchContextCommand(
    ScopeLevel TargetLevel,
    Guid? TargetTenantId,
    Guid? TargetCompanyId,
    Guid? TargetUnitId,
    string IpAddress,
    string UserAgent) : IRequest<Result<TokenPair>>;
