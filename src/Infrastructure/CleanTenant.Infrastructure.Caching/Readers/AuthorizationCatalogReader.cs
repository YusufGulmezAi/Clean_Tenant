using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.Application.Features.Catalog.Roles;
using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Caching.Readers;

/// <summary>
/// <para>
/// <see cref="IAuthorizationCatalogReader"/>'ın hybrid cache + EF Core fallback
/// implementasyonu. Catalog DB'den projection DTO'lar üretir; Role ve Permission
/// entity'lerini cache'ler.
/// </para>
/// </summary>
public sealed class AuthorizationCatalogReader : IAuthorizationCatalogReader
{
    private readonly ICacheStore _cache;
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AuthorizationCatalogReader(ICacheStore cache, ICatalogDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PermissionListItem>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Permission.All,
            async ct => await _db.Permissions
                .IgnoreQueryFilters()
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .Select(p => new PermissionListItem(p.Id, p.Code, p.Description, p.Module))
                .ToListAsync(ct)
                .ContinueWith(task => (IReadOnlyList<PermissionListItem>)task.Result, ct),
            CacheOptions.ListShortLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<RoleListItem>> GetRolesByScopeAsync(int scopeLevel, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Role.AllByScope(scopeLevel),
            async ct =>
            {
                var rows = await _db.Roles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(r => r.Scope == (ScopeLevel)scopeLevel)
                    .Where(r => !r.IsDeleted)
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Scope,
                        r.Description,
                        r.IsBuiltIn,
                        PermissionCount = _db.RolePermissions.Count(rp => rp.RoleId == r.Id)
                    })
                    .ToListAsync(ct);

                var list = rows.Select(r => new RoleListItem(
                    r.Id,
                    r.Name ?? string.Empty,
                    r.Scope,
                    r.Description,
                    r.IsBuiltIn,
                    r.PermissionCount)).ToList();

                return (IReadOnlyList<RoleListItem>)list;
            },
            CacheOptions.ListShortLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<RoleDetail?> GetRoleDetailAsync(Guid roleId, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<RoleDetail?>(
            CacheKeys.Role.DetailById(roleId),
            async ct =>
            {
                var role = await _db.Roles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(r => r.Id == roleId && !r.IsDeleted)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Scope,
                        r.Description,
                        r.IsBuiltIn,
                        PermissionIds = _db.RolePermissions
                            .Where(rp => rp.RoleId == r.Id)
                            .Select(rp => rp.PermissionId)
                            .ToList()
                    })
                    .FirstOrDefaultAsync(ct);

                if (role is null) return null;

                return new RoleDetail(
                    role.Id,
                    role.Name ?? string.Empty,
                    role.Scope,
                    role.Description,
                    role.IsBuiltIn,
                    (IReadOnlyList<Guid>)role.PermissionIds);
            },
            CacheOptions.DetailMediumLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> GetPermissionsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Authorization.PermissionsByRole(roleId),
            async ct => await _db.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync(ct)
                .ContinueWith(task => (IReadOnlyList<Guid>)task.Result, ct),
            CacheOptions.DetailMediumLived,
            cancellationToken);
}
