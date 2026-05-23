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

namespace CleanTenant.Application.Features.CompanyUsers;

/// <summary>
/// <see cref="CreateCompanyUserCommand"/> handler.
/// Kullanıcıyı Identity üzerinden oluşturur, Company-scope rol atamalarını kaydeder.
/// Site varlığı Main DB'den (cross-DB) doğrulanır; User + UserRoleAssignment Catalog DB'dedir.
/// </summary>
public sealed class CreateCompanyUserCommandHandler : IRequestHandler<CreateCompanyUserCommand, Result<UserListItem>>
{
    private readonly ICatalogDbContext _db;
    private readonly IMainDbContext _main;
    private readonly IUserRepository _userRepo;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateCompanyUserCommandHandler(ICatalogDbContext db, IMainDbContext main, IUserRepository userRepo)
    {
        _db = db;
        _main = main;
        _userRepo = userRepo;
    }

    /// <inheritdoc />
    public async Task<Result<UserListItem>> Handle(
        CreateCompanyUserCommand command,
        CancellationToken cancellationToken)
    {
        // Site var mı ve doğru Yönetim altında mı? (Main DB tenant-scoped — System
        // operatörü bağlamında query filter boş döneceğinden IgnoreQueryFilters.)
        var companyExists = await _main.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(c => c.Id == command.CompanyId
                        && c.TenantId == command.TenantId
                        && !c.IsDeleted, cancellationToken);

        if (!companyExists)
            return Result<UserListItem>.Failure(
                Error.NotFound("COMPANY-001", "Site bulunamadı."));

        // E-posta tekrarlı mı?
        if (await _userRepo.EmailExistsAsync(command.Email, ct: cancellationToken))
            return Result<UserListItem>.Failure(
                Error.Conflict("USER-002", "Bu e-posta adresi zaten kayıtlı."));

        // Rol ID'leri doğrula: hepsi Company scope ve silinmemiş olmalı
        var roles = await _db.Roles
            .AsNoTracking()
            .Where(r => command.RoleIds.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (roles.Count != command.RoleIds.Count)
            return Result<UserListItem>.Failure(
                Error.Validation("USER-003", "Bir veya daha fazla rol bulunamadı."));

        var nonCompanyRoles = roles.Where(r => r.Scope != ScopeLevel.Company).ToList();
        if (nonCompanyRoles.Count > 0)
            return Result<UserListItem>.Failure(
                Error.Validation("USER-004",
                    $"Site kullanıcısına yalnız Company scope rolleri atanabilir: " +
                    string.Join(", ", nonCompanyRoles.Select(r => r.Name))));

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

        // Company-scope rol atamaları
        var now = DateTimeOffset.UtcNow;
        foreach (var role in roles)
        {
            _db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = role.Id,
                ScopeLevel = ScopeLevel.Company,
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
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
