using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.SharedKernel.Context;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.Roles;

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly ICatalogDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;

    public CreateRoleCommandHandler(ICatalogDbContext db, ICacheInvalidator cacheInvalidator)
    {
        _db = db;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Scope = (ScopeLevel)request.Scope,
            Description = request.Description,
            IsBuiltIn = false,
            IsDeleted = false
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidator.InvalidateAllRolesAsync(cancellationToken);

        return role.Id;
    }
}
