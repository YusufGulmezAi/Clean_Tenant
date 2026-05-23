using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Common.Authorization;

/// <summary>
/// Bir kullanıcının belirli bir hedef scope'taki etkin rol adlarını ve permission
/// kodlarını çözer. Login ve bağlam geçişi (switch) akışlarında kullanılan ortak
/// kaynak — izin çözümleme mantığını tek yerde toplar (v0.2.13.e).
/// </summary>
/// <remarks>
/// <para>
/// <b>Cascade kuralı:</b> hedef <see cref="ScopeLevel.Company"/> ise, tam-scope
/// (Company) atamalarına ek olarak kullanıcının <b>parent tenant'taki Tenant-scope</b>
/// atamaları da hesaba katılır. Böylece bir TenantAdmin (süper tenant kullanıcısı)
/// tenant'ın tüm sitelerinde tam yetkili olur. Tam-scope eşleşme her zaman korunur;
/// Tenant ataması olmayan sıradan bir company kullanıcısı bu kuraldan etkilenmez.
/// </para>
/// </remarks>
public interface IScopePermissionResolver
{
    /// <summary>Verilen kullanıcı + hedef scope için (roller, permission'lar) döner.</summary>
    Task<(List<string> Roles, List<string> Permissions)> ResolveAsync(
        Guid userId,
        ScopeLevel level,
        Guid? tenantId,
        Guid? companyId,
        Guid? unitId,
        CancellationToken cancellationToken);
}
