using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="GetUserDetailQuery"/> handler.
/// </summary>
public sealed class GetUserDetailQueryHandler : IRequestHandler<GetUserDetailQuery, Result<UserDetail>>
{
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUserDetailQueryHandler(ICatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<UserDetail>> Handle(GetUserDetailQuery query, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.UrlCode == query.UrlCode && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<UserDetail>.Failure(Error.NotFound("USER-001", "Kullanıcı bulunamadı."));

        // Scope'taki atamalar
        var assignmentsQuery = _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == user.Id && a.ScopeLevel == query.Scope);

        if (query.TenantId.HasValue)
            assignmentsQuery = assignmentsQuery.Where(a => a.TenantId == query.TenantId);
        if (query.CompanyId.HasValue)
            assignmentsQuery = assignmentsQuery.Where(a => a.CompanyId == query.CompanyId);

        var assignments = await (
            from a in assignmentsQuery
            join r in _db.Roles.AsNoTracking() on a.RoleId equals r.Id
            where !r.IsDeleted
            select new UserRoleAssignmentDetail(a.Id, r.Id, r.Name!, r.Scope, a.IsActive)
        ).ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var detail = new UserDetail(
            user.Id,
            user.UrlCode,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.PhoneNumber,
            IsActive: !user.IsDeleted,
            IsLocked: user.LockoutEnd.HasValue && user.LockoutEnd.Value > now,
            user.TwoFactorEnabled,
            user.LastLoginAt,
            assignments);

        return Result<UserDetail>.Success(detail);
    }
}
