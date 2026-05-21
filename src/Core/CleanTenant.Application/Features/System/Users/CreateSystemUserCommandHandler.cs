using CleanTenant.Application.Common.Identity;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="CreateSystemUserCommand"/> handler.
/// Kullanıcıyı Identity üzerinden oluşturur, System-scope rol atamalarını kaydeder.
/// </summary>
public sealed class CreateSystemUserCommandHandler : IRequestHandler<CreateSystemUserCommand, Result<UserListItem>>
{
    private readonly ICatalogDbContext _db;
    private readonly IUserRepository _userRepo;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateSystemUserCommandHandler(ICatalogDbContext db, IUserRepository userRepo)
    {
        _db = db;
        _userRepo = userRepo;
    }

    /// <inheritdoc />
    public async Task<Result<UserListItem>> Handle(
        CreateSystemUserCommand command,
        CancellationToken cancellationToken)
    {
        // E-posta tekrarlı mı?
        if (await _userRepo.EmailExistsAsync(command.Email, ct: cancellationToken))
            return Result<UserListItem>.Failure(
                Error.Conflict("USER-002", "Bu e-posta adresi zaten kayıtlı."));

        // Rol ID'leri doğrula: hepsi System scope ve silinmemiş olmalı
        var roles = await _db.Roles
            .AsNoTracking()
            .Where(r => command.RoleIds.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (roles.Count != command.RoleIds.Count)
            return Result<UserListItem>.Failure(
                Error.Validation("USER-003", "Bir veya daha fazla rol bulunamadı."));

        var nonSystemRoles = roles.Where(r => r.Scope != ScopeLevel.System).ToList();
        if (nonSystemRoles.Count > 0)
            return Result<UserListItem>.Failure(
                Error.Validation("USER-004",
                    $"Sistem kullanıcısına yalnız System scope rolleri atanabilir: " +
                    string.Join(", ", nonSystemRoles.Select(r => r.Name))));

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

        // System-scope rol atamaları
        var now = DateTimeOffset.UtcNow;
        foreach (var role in roles)
        {
            _db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = role.Id,
                ScopeLevel = ScopeLevel.System,
                TenantId = null,
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
