using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Districts;

internal sealed class GetDistrictsByProvinceQueryHandler : IRequestHandler<GetDistrictsByProvinceQuery, Result<IReadOnlyList<DistrictListItem>>>
{
    private readonly ILookUpCatalogReader _reader;

    public GetDistrictsByProvinceQueryHandler(ILookUpCatalogReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<IReadOnlyList<DistrictListItem>>> Handle(GetDistrictsByProvinceQuery request, CancellationToken ct)
    {
        var districts = await _reader.GetDistrictsByProvinceAsync(request.ProvinceId, ct);
        return Result<IReadOnlyList<DistrictListItem>>.Success(districts);
    }
}
