using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.BuildingTypes;

internal sealed class UpdateBuildingTypeCommandHandler : IRequestHandler<UpdateBuildingTypeCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public UpdateBuildingTypeCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateBuildingTypeCommand command, CancellationToken ct)
    {
        var buildingType = await _db.BuildingTypes.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (buildingType is null || buildingType.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "Yapı tipi bulunamadı."));

        var existingByName = await _db.BuildingTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == command.Name && x.Id != command.Id && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı yapı tipi zaten mevcut."));

        buildingType.Name = command.Name;
        buildingType.Description = command.Description;
        buildingType.UpdatedAt = DateTimeOffset.UtcNow;

        _db.BuildingTypes.Update(buildingType);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
