using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Parcels;

/// <summary>
/// <see cref="CreateParcelCommand"/> handler.
/// </summary>
public sealed class CreateParcelCommandHandler : IRequestHandler<CreateParcelCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;
    private readonly ITenantContext _tenantContext;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateParcelCommandHandler(IMainDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateParcelCommand command, CancellationToken cancellationToken)
    {
        var blockExists = await _db.Blocks
            .AnyAsync(b => b.Id == command.BlockId, cancellationToken);
        if (!blockExists)
            return Result<Guid>.Failure(Error.NotFound("BLOCK-NOT-FOUND", "Ada bulunamadı."));

        var nextSortOrder = await _db.Parcels
            .Where(p => p.BlockId == command.BlockId)
            .MaxAsync(p => (int?)p.SortOrder, cancellationToken) ?? 0;

        var parcel = new Parcel
        {
            TenantId = _tenantContext.TenantId!.Value,
            BlockId = command.BlockId,
            Name = command.Name,
            SortOrder = nextSortOrder + 1,
        };

        _db.Parcels.Add(parcel);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(parcel.Id);
    }
}
