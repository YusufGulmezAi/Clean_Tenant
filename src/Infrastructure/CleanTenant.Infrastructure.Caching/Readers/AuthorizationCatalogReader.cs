using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.Application.Features.Catalog.Roles;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Caching.Readers;

/// <summary>
/// <para>
/// <see cref="IAuthorizationCatalogReader"/>'ın hybrid cache + EF Core fallback
/// implementasyonu. Catalog DB'den projection DTO'lar üretir; Role ve Permission
/// entity'lerini cache'ler.
/// </para>
/// <para>
/// <b>v0.2.9 — IDbContextFactory'ye geçiş:</b> Blazor Server circuit'inde
/// paralel reader çağrılarında scoped DbContext concurrency hatası veriyordu.
/// </para>
/// </summary>
public sealed class AuthorizationCatalogReader : IAuthorizationCatalogReader
{
    private readonly ICacheStore _cache;
    private readonly IDbContextFactory<CatalogDbContext> _dbFactory;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AuthorizationCatalogReader(ICacheStore cache, IDbContextFactory<CatalogDbContext> dbFactory)
    {
        _cache = cache;
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PermissionListItem>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Permission.All,
            async ct =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var list = await db.Permissions
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Code)
                    .Select(p => new PermissionListItem(p.Id, p.Code, p.Description, p.Module, p.MinimumRoleScope))
                    .ToListAsync(ct);
                return (IReadOnlyList<PermissionListItem>)list;
            },
            CacheOptions.ListShortLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<RoleListItem>> GetRolesByScopeAsync(int scopeLevel, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Role.AllByScope(scopeLevel),
            async ct =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var rows = await db.Roles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(r => r.Scope == (ScopeLevel)scopeLevel)
                    .Where(r => !r.IsDeleted)
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        r.Id,
                        r.UrlCode,
                        r.Name,
                        r.Scope,
                        r.Description,
                        r.IsBuiltIn,
                        r.TenantId,
                        r.CompanyId,
                        PermissionCount = db.RolePermissions.Count(rp => rp.RoleId == r.Id)
                    })
                    .ToListAsync(ct);

                var list = rows.Select(r => new RoleListItem(
                    r.Id,
                    r.UrlCode,
                    r.Name ?? string.Empty,
                    r.Scope,
                    r.Description,
                    r.IsBuiltIn,
                    r.TenantId,
                    r.CompanyId,
                    r.PermissionCount)).ToList();

                return (IReadOnlyList<RoleListItem>)list;
            },
            CacheOptions.ListShortLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<RoleListItem>> GetRolesByScopeForTenantAsync(
        int scopeLevel,
        Guid tenantId,
        CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Role.AllByScopeForTenant(scopeLevel, tenantId),
            async ct =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var rows = await db.Roles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(r => r.Scope == (ScopeLevel)scopeLevel)
                    .Where(r => !r.IsDeleted)
                    .Where(r => r.TenantId == null || r.TenantId == tenantId)
                    .OrderBy(r => r.TenantId == null ? 0 : 1)
                    .ThenBy(r => r.Name)
                    .Select(r => new
                    {
                        r.Id,
                        r.UrlCode,
                        r.Name,
                        r.Scope,
                        r.Description,
                        r.IsBuiltIn,
                        r.TenantId,
                        r.CompanyId,
                        PermissionCount = db.RolePermissions.Count(rp => rp.RoleId == r.Id)
                    })
                    .ToListAsync(ct);

                var list = rows.Select(r => new RoleListItem(
                    r.Id,
                    r.UrlCode,
                    r.Name ?? string.Empty,
                    r.Scope,
                    r.Description,
                    r.IsBuiltIn,
                    r.TenantId,
                    r.CompanyId,
                    r.PermissionCount)).ToList();

                return (IReadOnlyList<RoleListItem>)list;
            },
            CacheOptions.ListShortLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<RoleDetail?> GetRoleDetailAsync(Guid roleId, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<RoleDetail?>(
            CacheKeys.Role.DetailById(roleId),
            ct => LoadRoleDetailAsync(r => r.Id == roleId, ct),
            CacheOptions.DetailMediumLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<RoleDetail?> GetRoleByUrlCodeAsync(string urlCode, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync<RoleDetail?>(
            CacheKeys.Role.DetailByUrlCode(urlCode),
            ct => LoadRoleDetailAsync(r => r.UrlCode == urlCode, ct),
            CacheOptions.DetailMediumLived,
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> GetPermissionsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.Authorization.PermissionsByRole(roleId),
            async ct =>
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var list = await db.RolePermissions
                    .AsNoTracking()
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync(ct);
                return (IReadOnlyList<Guid>)list;
            },
            CacheOptions.DetailMediumLived,
            cancellationToken);

    /// <summary>
    /// Tek rolü filtre ile (Id veya UrlCode) yükler. GetRoleDetailAsync ve
    /// GetRoleByUrlCodeAsync ortak yardımcı (v0.2.9.a).
    /// </summary>
    private async Task<RoleDetail?> LoadRoleDetailAsync(
        System.Linq.Expressions.Expression<Func<Domain.Identity.Authorization.Role, bool>> filter,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var role = await db.Roles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(filter)
            .Where(r => !r.IsDeleted)
            .Select(r => new
            {
                r.Id,
                r.UrlCode,
                r.Name,
                r.Scope,
                r.Description,
                r.IsBuiltIn,
                r.TenantId,
                r.CompanyId,
                PermissionIds = db.RolePermissions
                    .Where(rp => rp.RoleId == r.Id)
                    .Select(rp => rp.PermissionId)
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (role is null) return null;

        return new RoleDetail(
            role.Id,
            role.UrlCode,
            role.Name ?? string.Empty,
            role.Scope,
            role.Description,
            role.IsBuiltIn,
            role.TenantId,
            role.CompanyId,
            (IReadOnlyList<Guid>)role.PermissionIds);
    }
}
