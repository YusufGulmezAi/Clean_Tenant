using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.LookUp.BuildingTypes;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.BuildingTypes;

internal sealed class CreateBuildingTypeCommandHandler : IRequestHandler<CreateBuildingTypeCommand, Result<Guid>>
{
    private readonly ICatalogDbContext _db;

    public CreateBuildingTypeCommandHandler(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateBuildingTypeCommand command, CancellationToken ct)
    {
        var existingByName = await _db.BuildingTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == command.Name && !x.IsDeleted, cancellationToken: ct);

        if (existingByName is not null)
            return Result<Guid>.Failure(Error.Conflict("LOOKUP-DUPLICATE", $"'{command.Name}' adlı yapı tipi zaten mevcut."));

        var buildingType = new BuildingType
        {
            Name = command.Name,
            Description = command.Description,
        };

        _db.BuildingTypes.Add(buildingType);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(buildingType.Id);
    }
}
