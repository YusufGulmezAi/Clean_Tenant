using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Catalog.Permissions;

public sealed class AssignPermissionsToRoleCommandHandler : IRequestHandler<AssignPermissionsToRoleCommand>
{
    private readonly ICatalogDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;

    public AssignPermissionsToRoleCommandHandler(ICatalogDbContext db, ICacheInvalidator cacheInvalidator)
    {
        _db = db;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Unit> Handle(AssignPermissionsToRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
            throw new InvalidOperationException($"Role not found: {request.RoleId}");

        var existing = await _db.RolePermissions
            .Where(rp => rp.RoleId == request.RoleId)
            .ToListAsync(cancellationToken);

        foreach (var rolePermission in existing)
        {
            _db.RolePermissions.Remove(rolePermission);
        }

        foreach (var permissionId in request.PermissionIds)
        {
            _db.RolePermissions.Add(new()
            {
                RoleId = request.RoleId,
                PermissionId = permissionId,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedBy = null
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidator.InvalidateRoleAsync(request.RoleId, cancellationToken);

        return Unit.Value;
    }
}
