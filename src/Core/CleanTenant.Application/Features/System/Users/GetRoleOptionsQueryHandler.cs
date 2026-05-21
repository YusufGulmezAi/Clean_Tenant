using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="GetRoleOptionsQuery"/> handler. Scope ve context'e göre uygun
/// rolleri filtreler; silinmiş roller hariç tutulur.
/// </summary>
public sealed class GetRoleOptionsQueryHandler : IRequestHandler<GetRoleOptionsQuery, Result<IReadOnlyList<RoleOption>>>
{
    private readonly ICatalogDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetRoleOptionsQueryHandler(ICatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoleOption>>> Handle(
        GetRoleOptionsQuery query,
        CancellationToken cancellationToken)
    {
        var rolesQuery = _db.Roles
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Scope == query.Scope);

        rolesQuery = query.Scope switch
        {
            // System: yalnız global (TenantId = null) System rolleri
            ScopeLevel.System => rolesQuery.Where(r => r.TenantId == null),

            // Tenant: global Tenant rolleri + bu tenant'a özel roller
            ScopeLevel.Tenant => rolesQuery.Where(r =>
                r.TenantId == null ||
                r.TenantId == query.TenantId),

            // Company: global + tenant + company'ye ait Company rolleri
            ScopeLevel.Company => rolesQuery.Where(r =>
                r.TenantId == null ||
                r.TenantId == query.TenantId ||
                r.CompanyId == query.CompanyId),

            _ => rolesQuery,
        };

        var roles = await rolesQuery
            .OrderBy(r => r.Name)
            .Select(r => new RoleOption(r.Id, r.Name!, r.Description, r.IsBuiltIn))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<RoleOption>>.Success(roles);
    }
}
