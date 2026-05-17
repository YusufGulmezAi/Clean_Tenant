using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Identity.Authorization;

/// <summary>
/// <para>
/// Bir kullanıcının belirli bir scope'taki rol atamasını temsil eder. Aynı
/// kullanıcı birden çok satıra sahip olabilir (System scope'ta Developer +
/// Tenant A'da TenantAdmin + Company X'te hem Manager hem Accountant + Unit
/// 101'de Malik gibi).
/// </para>
/// <para>
/// <b>Scope tutarlılığı (DB CHECK constraint):</b>
/// <list type="bullet">
///   <item><c>ScopeLevel = System</c> → TenantId, CompanyId, UnitId hepsi <c>NULL</c></item>
///   <item><c>ScopeLevel = Tenant</c> → TenantId <c>NOT NULL</c>, CompanyId/UnitId <c>NULL</c></item>
///   <item><c>ScopeLevel = Company</c> → TenantId, CompanyId <c>NOT NULL</c>, UnitId <c>NULL</c></item>
///   <item><c>ScopeLevel = Unit</c> → hepsi <c>NOT NULL</c></item>
/// </list>
/// Veri tabanı seviyesinde dayatılır; uygulama kodu tek savunma değildir.
/// </para>
/// <para>
/// <b>Unique:</b> <c>(UserId, RoleId, ScopeLevel, TenantId, CompanyId, UnitId)</c>
/// bileşik; aynı kullanıcı aynı role'ü aynı scope'a iki kez alamaz.
/// </para>
/// </summary>
public sealed class UserRoleAssignment : BaseEntity
{
    /// <summary>Atamanın yapıldığı kullanıcı.</summary>
    public Guid UserId { get; set; }

    /// <summary>Atanan rol.</summary>
    public Guid RoleId { get; set; }

    /// <summary>Atamanın etkili olduğu kapsam seviyesi.</summary>
    public ScopeLevel ScopeLevel { get; set; }

    /// <summary>Tenant kapsamı (Tenant/Company/Unit scope'larında dolu; System'de null).</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Company kapsamı (Company/Unit scope'larında dolu; Tenant ve System'de null).</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Unit kapsamı (yalnız Unit scope'unda dolu; diğerlerinde null).</summary>
    public Guid? UnitId { get; set; }

    /// <summary>Atamanın gerçekleştirildiği an (UTC).</summary>
    public DateTimeOffset AssignedAt { get; set; }

    /// <summary>Atamayı yapan kullanıcı; sistem seed'iyse null.</summary>
    public Guid? AssignedBy { get; set; }

    /// <summary>
    /// Atamanın sona ereceği an (UTC); null ise süresiz. Geçici izinler için kullanılır
    /// (örn. "31 Aralık'a kadar tatil amirliği").
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Atama aktif mi. Silmek yerine pasif yapma seçeneği (denetim izi korunur).
    /// </summary>
    public bool IsActive { get; set; }
}
