using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="ListUsersQuery"/> handler. Scope'a göre UserRoleAssignment üzerinden
/// kullanıcıları filtreler; rol adlarını denormalize eder.
/// Tenant scope + TenantId verildiğinde hem tenant-scope hem company-scope atamalarını
/// birleştirir (kapsamlı tenant kullanıcı görünümü).
/// </summary>
public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, Result<IReadOnlyList<UserListItem>>>
{
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ListUsersQueryHandler(ICatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserListItem>>> Handle(
        ListUsersQuery query,
        CancellationToken cancellationToken)
    {
        // Tenant kapsamlı görünüm: tenant + company scope atamalarını birleştir
        var isTenantView = query.Scope == ScopeLevel.Tenant && query.TenantId.HasValue;

        IQueryable<UserRoleAssignment> assignmentFilter;
        if (isTenantView)
        {
            assignmentFilter = _db.UserRoleAssignments
                .AsNoTracking()
                .Where(a => a.TenantId == query.TenantId
                         && (a.ScopeLevel == ScopeLevel.Tenant || a.ScopeLevel == ScopeLevel.Company));
        }
        else
        {
            assignmentFilter = _db.UserRoleAssignments
                .AsNoTracking()
                .Where(a => a.IsActive && a.ScopeLevel == query.Scope);

            if (query.TenantId.HasValue)
                assignmentFilter = assignmentFilter.Where(a => a.TenantId == query.TenantId);

            if (query.CompanyId.HasValue)
                assignmentFilter = assignmentFilter.Where(a => a.CompanyId == query.CompanyId);
        }

        var userIds = await assignmentFilter
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
            return Result<IReadOnlyList<UserListItem>>.Success([]);

        // Kullanıcıları yükle + opsiyonel arama
        var usersQuery = _db.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && userIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLowerInvariant();
            usersQuery = usersQuery.Where(u =>
                (u.FirstName + " " + u.LastName).ToLowerInvariant().Contains(search) ||
                u.Email!.ToLowerInvariant().Contains(search));
        }

        var users = await usersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
            return Result<IReadOnlyList<UserListItem>>.Success([]);

        var filteredIds = users.Select(u => u.Id).ToList();

        // Rol adlarını çek (tenant görünümünde sadece aktif atamalar, her iki scope dahil)
        var roleData = isTenantView
            ? await (
                from a in _db.UserRoleAssignments.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on a.RoleId equals r.Id
                where a.IsActive
                   && a.TenantId == query.TenantId
                   && (a.ScopeLevel == ScopeLevel.Tenant || a.ScopeLevel == ScopeLevel.Company)
                   && filteredIds.Contains(a.UserId)
                   && !r.IsDeleted
                select new { a.UserId, RoleName = r.Name! }
              ).ToListAsync(cancellationToken)
            : await (
                from a in _db.UserRoleAssignments.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on a.RoleId equals r.Id
                where a.IsActive
                   && a.ScopeLevel == query.Scope
                   && filteredIds.Contains(a.UserId)
                   && (!query.TenantId.HasValue || a.TenantId == query.TenantId)
                   && (!query.CompanyId.HasValue || a.CompanyId == query.CompanyId)
                   && !r.IsDeleted
                select new { a.UserId, RoleName = r.Name! }
              ).ToListAsync(cancellationToken);

        var rolesByUser = roleData
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.RoleName).ToList());

        // Tenant görünümünde aktiflik: bu tenant altında en az bir aktif ataması var mı?
        // Diğer scope'larda: kullanıcının global IsActive bayrağı kullanılır.
        HashSet<Guid>? tenantActiveIds = isTenantView
            ? roleData.Select(x => x.UserId).ToHashSet()
            : null;

        var now = DateTimeOffset.UtcNow;
        var items = users.Select(u => new UserListItem(
            u.Id,
            u.UrlCode,
            u.FirstName,
            u.LastName,
            u.Email!,
            u.PhoneNumber,
            IsActive: tenantActiveIds?.Contains(u.Id) ?? u.IsActive,
            IsLocked: u.LockoutEnd.HasValue && u.LockoutEnd.Value > now,
            u.TwoFactorEnabled,
            u.LastLoginAt,
            rolesByUser.GetValueOrDefault(u.Id, [])
        )).ToList();

        return Result<IReadOnlyList<UserListItem>>.Success(items);
    }
}
