using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Neighborhoods;

internal sealed class GetNeighborhoodsByDistrictQueryHandler : IRequestHandler<GetNeighborhoodsByDistrictQuery, Result<IReadOnlyList<NeighborhoodListItem>>>
{
    private readonly ILookUpCatalogReader _reader;

    public GetNeighborhoodsByDistrictQueryHandler(ILookUpCatalogReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<IReadOnlyList<NeighborhoodListItem>>> Handle(GetNeighborhoodsByDistrictQuery request, CancellationToken ct)
    {
        var neighborhoods = await _reader.GetNeighborhoodsByDistrictAsync(request.DistrictId, ct);
        return Result<IReadOnlyList<NeighborhoodListItem>>.Success(neighborhoods);
    }
}
