using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <para>
/// <see cref="DeleteTenantCommand"/> handler. Soft delete:
/// </para>
/// <list type="number">
///   <item>Tenant.IsDeleted=true, DeletedAt, Status=Terminated.</item>
///   <item>İlgili UserRoleAssignment'lar IsActive=false.</item>
///   <item>Cache invalidate (tüm tenant + ilgili company listesi).</item>
/// </list>
/// <para>
/// Reactivate Faz 1.5+'da ayrı endpoint olarak gelir; şu an tek-yön.
/// </para>
/// </summary>
public sealed class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, Result>
{
    private readonly ICatalogDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IClock _clock;
    private readonly ILogger<DeleteTenantCommandHandler> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteTenantCommandHandler(
        ICatalogDbContext db,
        ICacheInvalidator cacheInvalidator,
        IClock clock,
        ILogger<DeleteTenantCommandHandler> logger)
    {
        _db = db;
        _cacheInvalidator = cacheInvalidator;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .Where(t => t.Id == command.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("TENANT-NOT-FOUND", "Yönetim bulunamadı."));
        }

        if (tenant.IsDeleted)
        {
            return Result.Failure(
                Error.Conflict("TENANT-ALREADY-DELETED", "Yönetim zaten silinmiş."));
        }

        var now = _clock.UtcNow;

        tenant.IsDeleted = true;
        tenant.DeletedAt = now;
        tenant.Status = TenantStatus.Terminated;

        // Tüm assignment'ları pasif yap (Tenant/Company/Unit scope dahil)
        var assignments = await _db.UserRoleAssignments
            .Where(a => a.TenantId == command.TenantId && a.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var a in assignments)
        {
            a.IsActive = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Cache invalidate (tüm tenant + global company listeleri)
        await _cacheInvalidator.InvalidateTenantAsync(command.TenantId, cancellationToken);
        await _cacheInvalidator.InvalidateAllTenantsAsync(cancellationToken);

        _logger.LogInformation(
            "Yönetim soft-deleted: {TenantName} (Id={TenantId}). {AssignmentCount} rol ataması pasif edildi.",
            tenant.Name, tenant.Id, assignments.Count);

        return Result.Success();
    }
}
