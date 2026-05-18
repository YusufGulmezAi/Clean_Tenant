using CleanTenant.Application.Common.Persistence;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.BuildingTypes;

internal sealed class DeleteBuildingTypeCommandHandler : IRequestHandler<DeleteBuildingTypeCommand, Result>
{
    private readonly ICatalogDbContext _db;

    public DeleteBuildingTypeCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteBuildingTypeCommand command, CancellationToken ct)
    {
        var buildingType = await _db.BuildingTypes.FindAsync(new object[] { command.Id }, cancellationToken: ct);

        if (buildingType is null || buildingType.IsDeleted)
            return Result.Failure(Error.NotFound("LOOKUP-NOT-FOUND", "Yapı tipi bulunamadı."));

        buildingType.IsDeleted = true;
        buildingType.DeletedAt = DateTimeOffset.UtcNow;

        _db.BuildingTypes.Update(buildingType);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
