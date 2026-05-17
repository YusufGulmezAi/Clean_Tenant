using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Bir kullanıcının erişebileceği scope seçeneğinin bilgi taşıyıcısı.
/// Login response'unun <c>AvailableScopes</c> listesinde ve switch-context
/// isteğinin parametresinde kullanılır.
/// </para>
/// <para>
/// İstemci tarafında scope seçici UI'da görüntü için <see cref="TenantName"/>,
/// <see cref="CompanyName"/>, <see cref="UnitLabel"/> doldurulur (DB lookup ile).
/// </para>
/// </summary>
/// <param name="Level">Scope seviyesi (System / Tenant / Company / Unit).</param>
/// <param name="TenantId">Tenant kapsamında ve altında doludur; System'de null.</param>
/// <param name="CompanyId">Company kapsamında ve altında doludur; Tenant/System'de null.</param>
/// <param name="UnitId">Unit kapsamında doludur; diğerlerinde null.</param>
/// <param name="TenantName">UI gösterimi için tenant adı (opsiyonel).</param>
/// <param name="CompanyName">UI gösterimi için şirket adı (opsiyonel).</param>
/// <param name="UnitLabel">UI gösterimi için bağımsız bölüm etiketi (opsiyonel).</param>
public sealed record ScopeOption(
    ScopeLevel Level,
    Guid? TenantId,
    Guid? CompanyId,
    Guid? UnitId,
    string? TenantName = null,
    string? CompanyName = null,
    string? UnitLabel = null);
