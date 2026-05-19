using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Catalog.Roles;

/// <summary>
/// Rol silme handler'ı (soft delete). v0.2.8.c'den itibaren built-in koruma
/// ve sahiplik kontrolleri uygulanır.
/// </summary>
public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
{
    private readonly ICatalogDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    public DeleteRoleCommandHandler(
        ICatalogDbContext db,
        ICacheInvalidator cacheInvalidator,
        ICurrentSessionAccessor sessionAccessor)
    {
        _db = db;
        _cacheInvalidator = cacheInvalidator;
        _sessionAccessor = sessionAccessor;
    }

    public async Task<Unit> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role is null)
            throw new InvalidOperationException($"Rol bulunamadı: {request.Id}");

        RoleAccessGuard.EnsureCanManageRole(_sessionAccessor.Current, role);

        if (role.IsBuiltIn)
            throw new InvalidOperationException("Built-in roller silinemez.");

        role.IsDeleted = true;

        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidator.InvalidateRoleAsync(request.Id, cancellationToken);

        return Unit.Value;
    }
}
