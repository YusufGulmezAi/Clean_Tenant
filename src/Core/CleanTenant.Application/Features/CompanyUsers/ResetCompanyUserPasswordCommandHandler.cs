using CleanTenant.Application.Common.Identity;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.CompanyUsers;

/// <summary>
/// <see cref="ResetCompanyUserPasswordCommand"/> handler. Önce hedef kullanıcının bu
/// site'de (TenantId + CompanyId) aktif Company-scope ataması olduğunu doğrular (sahiplik
/// guard'ı), ardından <see cref="IUserRepository"/> üzerinden şifreyi sıfırlar.
/// </summary>
public sealed class ResetCompanyUserPasswordCommandHandler
    : IRequestHandler<ResetCompanyUserPasswordCommand, Result>
{
    private readonly ICatalogDbContext _db;
    private readonly IUserRepository _userRepo;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ResetCompanyUserPasswordCommandHandler(ICatalogDbContext db, IUserRepository userRepo)
    {
        _db = db;
        _userRepo = userRepo;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ResetCompanyUserPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UrlCode == command.UserUrlCode && !u.IsDeleted, cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("USER-001", "Kullanıcı bulunamadı."));

        // Sahiplik guard'ı: hedef kullanıcı bu site'de aktif Company-scope atamaya sahip olmalı.
        var belongsToCompany = await _db.UserRoleAssignments
            .AnyAsync(a => a.UserId == user.Id
                        && a.ScopeLevel == ScopeLevel.Company
                        && a.TenantId == command.TenantId
                        && a.CompanyId == command.CompanyId
                        && a.IsActive, cancellationToken);

        if (!belongsToCompany)
            return Result.Failure(Error.NotFound("USER-011", "Bu site'de aktif atama bulunamadı."));

        var result = await _userRepo.ResetPasswordAsync(user, command.NewPassword, cancellationToken);
        if (!result.Success)
            return Result.Failure(Error.Validation("USER-008", string.Join(" ", result.Errors)));

        return Result.Success();
    }
}
