using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Buildings;

/// <summary>
/// <see cref="CreateBuildingCommand"/> handler.
/// </summary>
public sealed class CreateBuildingCommandHandler : IRequestHandler<CreateBuildingCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;
    private readonly ITenantContext _tenantContext;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBuildingCommandHandler(IMainDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateBuildingCommand command, CancellationToken cancellationToken)
    {
        var parcelExists = await _db.Parcels
            .AnyAsync(p => p.Id == command.ParcelId, cancellationToken);
        if (!parcelExists)
            return Result<Guid>.Failure(Error.NotFound("PARCEL-NOT-FOUND", "Parsel bulunamadı."));

        var nextSortOrder = await _db.Buildings
            .Where(b => b.ParcelId == command.ParcelId)
            .MaxAsync(b => (int?)b.SortOrder, cancellationToken) ?? 0;

        var building = new Building
        {
            TenantId = _tenantContext.TenantId!.Value,
            ParcelId = command.ParcelId,
            Name = command.Name,
            MunicipalNo = command.MunicipalNo,
            Type = command.Type,
            SortOrder = nextSortOrder + 1,
        };

        _db.Buildings.Add(building);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(building.Id);
    }
}
