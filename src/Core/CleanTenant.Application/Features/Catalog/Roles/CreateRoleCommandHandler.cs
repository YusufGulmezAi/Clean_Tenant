using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.SharedKernel.Context;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.Roles;

/// <summary>
/// Yeni rol oluşturma handler'ı. v0.2.8.c'den itibaren scope ceiling ve
/// sahiplik (TenantId/CompanyId) güvenlik kontrolleri uygulanır.
/// </summary>
public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly ICatalogDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    public CreateRoleCommandHandler(
        ICatalogDbContext db,
        ICacheInvalidator cacheInvalidator,
        ICurrentSessionAccessor sessionAccessor)
    {
        _db = db;
        _cacheInvalidator = cacheInvalidator;
        _sessionAccessor = sessionAccessor;
    }

    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current;
        var requestedScope = (ScopeLevel)request.Scope;

        RoleAccessGuard.EnsureCanCreateAtScope(session, requestedScope);

        // System scope assigner serbestçe TenantId/CompanyId belirler.
        // Tenant/Company scope assigner'lar zorla kendi tenant/company'leriyle bağlar.
        Guid? tenantId;
        Guid? companyId;
        if (session!.ScopeLevel == ScopeLevel.System)
        {
            tenantId = request.TenantId;
            companyId = request.CompanyId;
        }
        else if (session.ScopeLevel == ScopeLevel.Tenant)
        {
            tenantId = session.TenantId;
            companyId = null;
        }
        else // Company veya Unit
        {
            tenantId = session.TenantId;
            companyId = session.CompanyId;
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Scope = requestedScope,
            Description = request.Description,
            TenantId = tenantId,
            CompanyId = companyId,
            IsBuiltIn = false,
            IsDeleted = false
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidator.InvalidateAllRolesAsync(cancellationToken);

        return role.Id;
    }
}
