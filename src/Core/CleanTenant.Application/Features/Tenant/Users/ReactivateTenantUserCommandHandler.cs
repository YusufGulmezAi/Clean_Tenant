using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Tenant.Users;

/// <summary>
/// <see cref="ReactivateTenantUserCommand"/> handler.
/// </summary>
public sealed class ReactivateTenantUserCommandHandler : IRequestHandler<ReactivateTenantUserCommand, Result>
{
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReactivateTenantUserCommandHandler(ICatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateTenantUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UrlCode == command.UserUrlCode && !u.IsDeleted, cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("USER-001", "Kullanıcı bulunamadı."));

        var assignments = await _db.UserRoleAssignments
            .Where(a => a.UserId == user.Id
                     && a.ScopeLevel == ScopeLevel.Tenant
                     && a.TenantId == command.TenantId
                     && !a.IsActive)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
            return Result.Failure(Error.NotFound("USER-012", "Bu tenant'ta pasif atama bulunamadı."));

        foreach (var assignment in assignments)
        {
            assignment.IsActive = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
