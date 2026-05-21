using CleanTenant.Application.Common.Identity;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="UpdateUserCommand"/> handler.
/// Profil alanlarını günceller; mevcut scope atamalarını komuttaki RoleIds ile eşler.
/// </summary>
public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserListItem>>
{
    private readonly ICatalogDbContext _db;
    private readonly IUserRepository _userRepo;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateUserCommandHandler(ICatalogDbContext db, IUserRepository userRepo)
    {
        _db = db;
        _userRepo = userRepo;
    }

    /// <inheritdoc />
    public async Task<Result<UserListItem>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(u => u.UrlCode == command.UrlCode && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<UserListItem>.Failure(Error.NotFound("USER-001", "Kullanıcı bulunamadı."));

        // E-posta değiştiyse tekrar kontrolü
        if (!string.Equals(user.Email, command.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _userRepo.EmailExistsAsync(command.Email, excludeUserId: user.Id, ct: cancellationToken))
                return Result<UserListItem>.Failure(
                    Error.Conflict("USER-002", "Bu e-posta adresi zaten kayıtlı."));
        }

        // Rolleri doğrula
        var roles = await _db.Roles
            .AsNoTracking()
            .Where(r => command.RoleIds.Contains(r.Id) && !r.IsDeleted && r.Scope == command.Scope)
            .ToListAsync(cancellationToken);

        if (roles.Count != command.RoleIds.Count)
            return Result<UserListItem>.Failure(
                Error.Validation("USER-003", "Bir veya daha fazla rol bulunamadı veya geçersiz scope."));

        // Profil güncelle
        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        user.Email = command.Email;
        user.UserName = command.Email;
        user.NormalizedEmail = command.Email.ToUpperInvariant();
        user.NormalizedUserName = command.Email.ToUpperInvariant();
        user.PhoneNumber = command.PhoneNumber;

        var updateResult = await _userRepo.UpdateAsync(user, cancellationToken);
        if (!updateResult.Success)
            return Result<UserListItem>.Failure(
                Error.Validation("USER-006", string.Join(" ", updateResult.Errors)));

        // Scope'taki mevcut atamaları sil; yenileri ekle
        var existingAssignments = await _db.UserRoleAssignments
            .Where(a => a.UserId == user.Id && a.ScopeLevel == command.Scope
                     && (!command.TenantId.HasValue || a.TenantId == command.TenantId)
                     && (!command.CompanyId.HasValue || a.CompanyId == command.CompanyId))
            .ToListAsync(cancellationToken);

        // Kaldırılacaklar
        var toRemove = existingAssignments
            .Where(a => !command.RoleIds.Contains(a.RoleId))
            .ToList();
        _db.UserRoleAssignments.RemoveRange(toRemove);

        // Eklenecekler
        var existingRoleIds = existingAssignments.Select(a => a.RoleId).ToHashSet();
        var now = DateTimeOffset.UtcNow;
        foreach (var role in roles.Where(r => !existingRoleIds.Contains(r.Id)))
        {
            _db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = role.Id,
                ScopeLevel = command.Scope,
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                AssignedAt = now,
                IsActive = true,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var now2 = DateTimeOffset.UtcNow;
        return Result<UserListItem>.Success(new UserListItem(
            user.Id,
            user.UrlCode,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.PhoneNumber,
            IsActive: !user.IsDeleted,
            IsLocked: user.LockoutEnd.HasValue && user.LockoutEnd.Value > now2,
            user.TwoFactorEnabled,
            user.LastLoginAt,
            roles.Select(r => r.Name!).ToList()));
    }
}
