namespace CleanTenant.Domain.Identity.Authorization;

/// <summary>
/// <para>
/// Rol ve Permission arasındaki çok-çoka join entity'si. Bir <see cref="Role"/>
/// birden çok <see cref="Permission"/>'a sahip olabilir; bir Permission birden
/// çok role atanabilir.
/// </para>
/// <para>
/// <b>Composite PK:</b> <c>(RoleId, PermissionId)</c>. Detail entity olduğu
/// için URL kodu, soft-delete, RowVersion taşımaz. Audit ihtiyacı yalnız
/// "kim atadı / ne zaman atadı" düzeyinde minimaldir.
/// </para>
/// </summary>
public sealed class RolePermission
{
    /// <summary>Bağlanan rol.</summary>
    public Guid RoleId { get; set; }

    /// <summary>Atanan permission.</summary>
    public Guid PermissionId { get; set; }

    /// <summary>Atama anı (UTC).</summary>
    public DateTimeOffset GrantedAt { get; set; }

    /// <summary>Atamayı yapan kullanıcı; sistem seed'iyse null.</summary>
    public Guid? GrantedBy { get; set; }
}
