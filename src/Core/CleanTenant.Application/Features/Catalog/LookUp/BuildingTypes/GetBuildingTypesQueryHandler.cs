using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.BuildingTypes;

internal sealed class GetBuildingTypesQueryHandler : IRequestHandler<GetBuildingTypesQuery, Result<IReadOnlyList<BuildingTypeListItem>>>
{
    private readonly ILookUpCatalogReader _reader;

    public GetBuildingTypesQueryHandler(ILookUpCatalogReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<IReadOnlyList<BuildingTypeListItem>>> Handle(GetBuildingTypesQuery request, CancellationToken ct)
    {
        var buildingTypes = await _reader.GetBuildingTypesAsync(ct);
        return Result<IReadOnlyList<BuildingTypeListItem>>.Success(buildingTypes);
    }
}
