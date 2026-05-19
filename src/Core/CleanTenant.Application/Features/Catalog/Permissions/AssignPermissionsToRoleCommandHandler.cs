using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Catalog.Permissions;

/// <summary>
/// <para>
/// Bir role izin set'ini bulk replace eder (önce mevcutları siler, sonra
/// gelenleri ekler). v0.2.8.c'den itibaren üç katmanlı güvenlik kontrolü
/// uygulanır:
/// </para>
/// <list type="number">
///   <item><b>Sahiplik:</b> Rolü sadece System veya rolün sahibi tenant/company yönetebilir.</item>
///   <item><b>Privilege ceiling:</b> Atanan tüm izinler assigner'ın kendi izin setinde olmalı.</item>
///   <item><b>Scope ceiling:</b> İzinlerin <c>MinimumRoleScope</c>'u rolün scope'una uyumlu olmalı.</item>
/// </list>
/// </summary>
public sealed class AssignPermissionsToRoleCommandHandler : IRequestHandler<AssignPermissionsToRoleCommand>
{
    private readonly ICatalogDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    public AssignPermissionsToRoleCommandHandler(
        ICatalogDbContext db,
        ICacheInvalidator cacheInvalidator,
        ICurrentSessionAccessor sessionAccessor)
    {
        _db = db;
        _cacheInvalidator = cacheInvalidator;
        _sessionAccessor = sessionAccessor;
    }

    public async Task<Unit> Handle(AssignPermissionsToRoleCommand request, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new UnauthorizedAccessException("Oturum bulunamadı.");

        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
            throw new InvalidOperationException($"Rol bulunamadı: {request.RoleId}");

        // 1) Sahiplik kontrolü — System veya rolün sahibi tenant/company
        RoleAccessGuard.EnsureCanManageRole(session, role);

        // İstenen izinleri (entity olarak) yükle; hem code hem MinimumRoleScope için.
        var requestedPermissions = await _db.Permissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // İstenen ID'lerden bazıları DB'de yoksa hata.
        if (requestedPermissions.Count != request.PermissionIds.Count)
            throw new InvalidOperationException("Geçersiz permission ID(leri) geldi.");

        var requestedCodes = requestedPermissions.Select(p => p.Code).ToList();

        // 2) Privilege ceiling — assigner sadece kendi izinlerini dağıtabilir
        RoleAccessGuard.EnsurePermissionCeiling(session, requestedCodes);

        // 3) Scope ceiling — izinler rolün scope'una uyumlu olmalı
        RoleAccessGuard.EnsureScopeCeiling(role.Scope, requestedPermissions);

        // Bulk replace
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
                GrantedBy = session.UserId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidator.InvalidateRoleAsync(request.RoleId, cancellationToken);

        return Unit.Value;
    }
}
