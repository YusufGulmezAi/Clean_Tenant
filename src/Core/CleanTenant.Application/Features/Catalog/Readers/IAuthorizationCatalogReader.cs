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
    /// </summary>
    Task<IReadOnlyList<RoleListItem>> GetRolesByScopeAsync(int scopeLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single role by ID with its permission list.
    /// Cache: medium-lived detail.
    /// </summary>
    Task<RoleDetail?> GetRoleDetailAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get permission IDs assigned to a role (fast lookup for authorization checks).
    /// Cache: medium-lived list.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetPermissionsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
