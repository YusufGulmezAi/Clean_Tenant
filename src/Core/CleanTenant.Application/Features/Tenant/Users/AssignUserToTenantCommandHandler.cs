using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.System.Users;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Tenant.Users;

/// <summary>
/// <see cref="AssignUserToTenantCommand"/> handler.
/// Mevcut kullanıcıyı bulur, Tenant-scope rolleri doğrular, atama kaydeder.
/// </summary>
public sealed class AssignUserToTenantCommandHandler : IRequestHandler<AssignUserToTenantCommand, Result<UserListItem>>
{
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AssignUserToTenantCommandHandler(ICatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<UserListItem>> Handle(
        AssignUserToTenantCommand command,
        CancellationToken cancellationToken)
    {
        // Tenant var mı?
        var tenantExists = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == command.TenantId && !t.IsDeleted, cancellationToken);

        if (!tenantExists)
            return Result<UserListItem>.Failure(
                Error.NotFound("TENANT-001", "Yönetim bulunamadı."));

        // Kullanıcıyı e-posta ile bul
        var user = await _db.Users
            .Where(u => u.Email == command.Email && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<UserListItem>.Failure(
                Error.NotFound("USER-001", $"'{command.Email}' e-postasıyla kayıtlı kullanıcı bulunamadı."));

        if (!user.IsActive)
            return Result<UserListItem>.Failure(
                Error.Conflict("USER-008", "Kullanıcı devre dışı bırakılmış. Önce devreye alın."));

        // Rol ID'leri doğrula: Tenant scope ve silinmemiş olmalı
        var roles = await _db.Roles
            .AsNoTracking()
            .Where(r => command.RoleIds.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (roles.Count != command.RoleIds.Count)
            return Result<UserListItem>.Failure(
                Error.Validation("USER-003", "Bir veya daha fazla rol bulunamadı."));

        var nonTenantRoles = roles.Where(r => r.Scope != ScopeLevel.Tenant).ToList();
        if (nonTenantRoles.Count > 0)
            return Result<UserListItem>.Failure(
                Error.Validation("USER-004",
                    $"Yönetim kullanıcısına yalnız Tenant scope rolleri atanabilir: " +
                    string.Join(", ", nonTenantRoles.Select(r => r.Name))));

        // Mevcut aktif atamaları bul — aynı tenant için zaten atanmış rolleri güncelle
        var existing = await _db.UserRoleAssignments
            .Where(a => a.UserId == user.Id
                     && a.ScopeLevel == ScopeLevel.Tenant
                     && a.TenantId == command.TenantId
                     && a.IsActive)
            .ToListAsync(cancellationToken);

        // Zaten tüm roller atanmış mı?
        var existingRoleIds = existing.Select(a => a.RoleId).ToHashSet();
        var newRoleIds = command.RoleIds.Where(id => !existingRoleIds.Contains(id)).ToList();

        if (newRoleIds.Count == 0)
            return Result<UserListItem>.Failure(
                Error.Conflict("USER-010", "Kullanıcı bu Tenant'a seçilen rollerle zaten atanmış."));

        // Yeni rol atamalarını ekle
        var now = DateTimeOffset.UtcNow;
        var newRoles = roles.Where(r => newRoleIds.Contains(r.Id)).ToList();
        foreach (var role in newRoles)
        {
            _db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = role.Id,
                ScopeLevel = ScopeLevel.Tenant,
                TenantId = command.TenantId,
                CompanyId = null,
                UnitId = null,
                AssignedAt = now,
                IsActive = true,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Güncel rol listesi (mevcut + yeni)
        var allRoles = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == user.Id
                     && a.ScopeLevel == ScopeLevel.Tenant
                     && a.TenantId == command.TenantId
                     && a.IsActive)
            .Join(_db.Roles, a => a.RoleId, r => r.Id, (_, r) => r.Name!)
            .ToListAsync(cancellationToken);

        var item = new UserListItem(
            user.Id,
            user.UrlCode,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.PhoneNumber,
            IsActive: user.IsActive,
            IsLocked: false,
            TwoFactorEnabled: user.TwoFactorEnabled,
            LastLoginAt: user.LastLoginAt,
            RoleNames: allRoles);

        return Result<UserListItem>.Success(item);
    }
}
