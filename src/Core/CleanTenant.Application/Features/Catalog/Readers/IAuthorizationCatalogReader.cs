using CleanTenant.Application.Features.Catalog.Roles;

namespace CleanTenant.Application.Features.Catalog.Readers;

/// <summary>
/// Read-side access to Role and Permission entities from the Catalog DB.
/// Hybrid caching strategy: all queries are cached with short/medium TTLs
/// and fall back to EF Core if cache misses.
/// </summary>
public interface IAuthorizationCatalogReader
{
    /// <summary>
    /// Get all permissions ordered by Module, then Code.
    /// Cache: short-lived list (CacheOptions.ListShortLived).
    /// </summary>
    Task<IReadOnlyList<PermissionListItem>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all roles for a specific scope level (System / Tenant / Company / Unit).
    /// Cache: short-lived list per scope.
    /// System operator görünümü — tüm tenant'ların rolleri dahil.
    /// </summary>
    Task<IReadOnlyList<RoleListItem>> GetRolesByScopeAsync(int scopeLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>
    /// Get all roles for a specific scope level filtered for a tenant context.
    /// Returns: global roles (TenantId=null) + roles owned by the given tenant.
    /// Cache: short-lived per (scope, tenantId) pair (v0.2.8.d).
    /// </para>
    /// <para>
    /// TenantAdmin / CompanyAdmin görünümü için kullanılır. Diğer tenant'ların
    /// custom rolleri görünmez.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<RoleListItem>> GetRolesByScopeForTenantAsync(
        int scopeLevel,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single role by ID with its permission list.
    /// Cache: medium-lived detail.
    /// </summary>
    Task<RoleDetail?> GetRoleDetailAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single role by UrlCode (9 karakter Base58) with its permission list.
    /// Cache: medium-lived detail (v0.2.9.a — UI rotaları GUID yerine UrlCode kullanır).
    /// </summary>
    Task<RoleDetail?> GetRoleByUrlCodeAsync(string urlCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get permission IDs assigned to a role (fast lookup for authorization checks).
    /// Cache: medium-lived list.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetPermissionsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
