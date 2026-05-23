using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Common.Authorization;

/// <summary>
/// <see cref="IScopePermissionResolver"/>'in Catalog DB implementasyonu. İzinler
/// <c>UserRoleAssignments → Roles → RolePermissions → Permissions</c> zinciriyle
/// çözülür. Cascade yalnız Company hedefinde parent Tenant atamalarını ekler.
/// </summary>
public sealed class ScopePermissionResolver : IScopePermissionResolver
{
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılığını alır.</summary>
    public ScopePermissionResolver(ICatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<(List<string> Roles, List<string> Permissions)> ResolveAsync(
        Guid userId,
        ScopeLevel level,
        Guid? tenantId,
        Guid? companyId,
        Guid? unitId,
        CancellationToken cancellationToken)
    {
        // Cascade yalnız Company hedefinde devreye girer (tenant → tüm siteler).
        var cascadeToTenant = level == ScopeLevel.Company;

        var matched = _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                     && a.IsActive
                     && (
                            // Tam-scope eşleşme — mevcut davranış birebir korunur.
                            (a.ScopeLevel == level
                             && a.TenantId == tenantId
                             && a.CompanyId == companyId
                             && a.UnitId == unitId)
                            // Cascade: parent tenant'ın Tenant-scope ataması (yalnız Company hedefi).
                            || (cascadeToTenant
                                && a.ScopeLevel == ScopeLevel.Tenant
                                && a.TenantId == tenantId
                                && a.CompanyId == null
                                && a.UnitId == null)
                        ));

        var roles = await matched
            .Join(_db.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissions = await matched
            .Join(_db.RolePermissions, a => a.RoleId, rp => rp.RoleId, (a, rp) => rp.PermissionId)
            .Join(_db.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return (roles, permissions);
    }
}
