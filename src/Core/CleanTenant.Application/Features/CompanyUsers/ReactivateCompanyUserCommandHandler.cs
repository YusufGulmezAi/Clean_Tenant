using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.CompanyUsers;

/// <summary>
/// <see cref="ReactivateCompanyUserCommand"/> handler.
/// </summary>
public sealed class ReactivateCompanyUserCommandHandler : IRequestHandler<ReactivateCompanyUserCommand, Result>
{
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReactivateCompanyUserCommandHandler(ICatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateCompanyUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UrlCode == command.UserUrlCode && !u.IsDeleted, cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("USER-001", "Kullanıcı bulunamadı."));

        var assignments = await _db.UserRoleAssignments
            .Where(a => a.UserId == user.Id
                     && a.ScopeLevel == ScopeLevel.Company
                     && a.TenantId == command.TenantId
                     && a.CompanyId == command.CompanyId
                     && !a.IsActive)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
            return Result.Failure(Error.NotFound("USER-012", "Bu site'de pasif atama bulunamadı."));

        foreach (var assignment in assignments)
        {
            assignment.IsActive = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
