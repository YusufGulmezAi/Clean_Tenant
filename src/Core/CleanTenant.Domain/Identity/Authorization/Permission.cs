using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Identity.Authorization;

/// <summary>
/// <para>
/// Granular bir yetki tanımı. Her permission bir aksiyona izin verir
/// (örn. <c>"Invoice.Approve"</c>, <c>"Tenant.Read"</c>). Roller permission'lara
/// <see cref="RolePermission"/> üzerinden bağlanır.
/// </para>
/// <para>
/// <b>URL'i yoktur:</b> <see cref="Code"/> kendisi insan-okunabilir kalıcı bir
/// tanımlayıcıdır; ayrı bir URL kod gerekmez.
/// </para>
/// </summary>
public sealed class Permission : BaseEntity
{
    /// <summary>
    /// İnsan-okunabilir permission kodu. Sözdizimi:
    /// <c>"&lt;Module&gt;.&lt;Action&gt;[.&lt;Qualifier&gt;]"</c> —
    /// örn. <c>"Invoice.Approve"</c>, <c>"Tenant.Read"</c>,
    /// <c>"User.Manage.Tenant"</c>. Sistem genelinde unique.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Permission'ın ne işe yaradığının açıklaması (UI'da gösterim için).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Permission'ın ait olduğu modül (gruplama amaçlı). Örn. <c>"Identity"</c>,
    /// <c>"Billing"</c>, <c>"Reporting"</c>. Rol Yönetimi ekranında permission
    /// listesini modül başlıkları altında göstermek için kullanılır.
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// <para>
    /// Bu izni tutabilmesi için bir rolün en az hangi scope seviyesine sahip
    /// olması gerektiğini belirtir. ScopeLevel hiyerarşisi
    /// (System=1 &lt; Tenant=2 &lt; Company=3 &lt; Unit=4) küçük değer = geniş
    /// yetki olarak yorumlanır.
    /// </para>
    /// <para>
    /// <b>Filtre kuralı:</b> <c>role.Scope &lt;= permission.MinimumRoleScope</c>
    /// olduğunda izin role atanabilir. Örnekler:
    /// <list type="bullet">
    ///   <item><c>Tenant.Create</c> → System: yalnız System rolleri tutabilir</item>
    ///   <item><c>User.Read</c> → Tenant: System ve Tenant rolleri tutabilir</item>
    ///   <item><c>Unit.Read</c> → Unit: tüm scope'lar tutabilir</item>
    /// </list>
    /// </para>
    /// </summary>
    public ScopeLevel MinimumRoleScope { get; set; } = ScopeLevel.Unit;
}
