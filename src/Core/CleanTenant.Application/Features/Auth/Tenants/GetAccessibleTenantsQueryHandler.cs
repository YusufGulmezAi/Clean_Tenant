using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenantEntity = CleanTenant.Domain.Identity.Tenants.Tenant;
using TenantStatus = CleanTenant.Domain.Identity.Tenants.TenantStatus;

namespace CleanTenant.Application.Features.Auth.Tenants;

/// <summary>
/// <see cref="GetAccessibleTenantsQuery"/> handler'ı. Mevcut session'a göre
/// tenant kümesi farklılaşır: System scope tüm Active tenant'ları görür;
/// alt scope'lar yalnız UserRoleAssignments üzerindeki distinct tenant'lara.
/// </summary>
public sealed class GetAccessibleTenantsQueryHandler
    : IRequestHandler<GetAccessibleTenantsQuery, Result<IReadOnlyList<AccessibleTenant>>>
{
    private readonly ICatalogDbContext _db;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetAccessibleTenantsQueryHandler(
        ICatalogDbContext db,
        ICurrentSessionAccessor sessionAccessor)
    {
        _db = db;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Erişilebilir tenant listesini döner.</summary>
    public async Task<Result<IReadOnlyList<AccessibleTenant>>> Handle(
        GetAccessibleTenantsQuery query,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current;
        if (session is null)
        {
            return Result<IReadOnlyList<AccessibleTenant>>.Failure(
                Error.Unauthorized("AUTH-005", "Kimlik bilgisi gerekli."));
        }

        // System scope kullanıcı tüm Active tenant'ları görebilir.
        IQueryable<TenantEntity> source = _db.Tenants.AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active);

        if (session.ScopeLevel != ScopeLevel.System)
        {
            // Alt scope'lar yalnız kendi rol atamasına sahip tenant'lar.
            var assignedTenantIds = _db.UserRoleAssignments
                .AsNoTracking()
                .Where(a => a.UserId == session.UserId && a.IsActive && a.TenantId != null)
                .Select(a => a.TenantId!.Value)
                .Distinct();

            source = source.Where(t => assignedTenantIds.Contains(t.Id));
        }

        var tenants = await source
            .OrderBy(t => t.Name)
            .Select(t => new AccessibleTenant(t.Id, t.UrlCode, t.Name))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AccessibleTenant>>.Success(tenants);
    }
}
