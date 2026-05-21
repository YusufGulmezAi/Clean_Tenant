using CleanTenant.Application.Common.Identity;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.System.Users;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Tenant.Users;

/// <summary>
/// <see cref="CreateTenantUserCommand"/> handler.
/// Kullanıcıyı Identity üzerinden oluşturur, Tenant-scope rol atamalarını kaydeder.
/// </summary>
public sealed class CreateTenantUserCommandHandler : IRequestHandler<CreateTenantUserCommand, Result<UserListItem>>
{
    private readonly ICatalogDbContext _db;
    private readonly IUserRepository _userRepo;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateTenantUserCommandHandler(ICatalogDbContext db, IUserRepository userRepo)
    {
        _db = db;
        _userRepo = userRepo;
    }

    /// <inheritdoc />
    public async Task<Result<UserListItem>> Handle(
        CreateTenantUserCommand command,
        CancellationToken cancellationToken)
    {
        // Tenant var mı?
        var tenantExists = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == command.TenantId && !t.IsDeleted, cancellationToken);

        if (!tenantExists)
            return Result<UserListItem>.Failure(
                Error.NotFound("TENANT-001", "Yönetim bulunamadı."));

        // E-posta tekrarlı mı?
        if (await _userRepo.EmailExistsAsync(command.Email, ct: cancellationToken))
            return Result<UserListItem>.Failure(
                Error.Conflict("USER-002", "Bu e-posta adresi zaten kayıtlı."));

        // Rol ID'leri doğrula: hepsi Tenant scope ve silinmemiş olmalı
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

        // Kullanıcı oluştur
        var user = new User
        {
            UserName = command.Email,
            Email = command.Email,
            EmailConfirmed = true,
            PhoneNumber = command.PhoneNumber,
            FirstName = command.FirstName,
            LastName = command.LastName,
            TwoFactorEnabled = false,
            RequiresPasswordChange = true,
        };

        var createResult = await _userRepo.CreateAsync(user, command.Password, cancellationToken);
        if (!createResult.Success)
            return Result<UserListItem>.Failure(
                Error.Validation("USER-005", string.Join(" ", createResult.Errors)));

        // Tenant-scope rol atamaları
        var now = DateTimeOffset.UtcNow;
        foreach (var role in roles)
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

        var item = new UserListItem(
            user.Id,
            user.UrlCode,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.PhoneNumber,
            IsActive: true,
            IsLocked: false,
            TwoFactorEnabled: false,
            LastLoginAt: null,
            RoleNames: roles.Select(r => r.Name!).ToList());

        return Result<UserListItem>.Success(item);
    }
}
