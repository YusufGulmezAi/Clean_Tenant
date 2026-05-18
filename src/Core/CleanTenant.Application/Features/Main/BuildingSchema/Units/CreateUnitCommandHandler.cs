using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainUnit = CleanTenant.Domain.Tenant.BuildingSchema.Unit;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Units;

/// <summary>
/// <see cref="CreateUnitCommand"/> handler.
/// </summary>
public sealed class CreateUnitCommandHandler : IRequestHandler<CreateUnitCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;
    private readonly ITenantContext _tenantContext;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateUnitCommandHandler(IMainDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateUnitCommand command, CancellationToken cancellationToken)
    {
        var buildingExists = await _db.Buildings
            .AnyAsync(b => b.Id == command.BuildingId, cancellationToken);
        if (!buildingExists)
            return Result<Guid>.Failure(Error.NotFound("BUILDING-NOT-FOUND", "Yapı bulunamadı."));

        var nextSortOrder = await _db.Units
            .Where(u => u.BuildingId == command.BuildingId)
            .MaxAsync(u => (int?)u.SortOrder, cancellationToken) ?? 0;

        var unit = new DomainUnit
        {
            TenantId = _tenantContext.TenantId!.Value,
            BuildingId = command.BuildingId,
            Number = command.Number,
            NationalAddressCode = command.NationalAddressCode,
            Type = command.Type,
            SquareMeters = command.SquareMeters,
            LandShare = command.LandShare,
            AllocatedArea = command.AllocatedArea,
            Floor = command.Floor,
            Orientation = command.Orientation,
            Layout = command.Layout,
            SortOrder = nextSortOrder + 1,
        };

        _db.Units.Add(unit);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(unit.Id);
    }
}
