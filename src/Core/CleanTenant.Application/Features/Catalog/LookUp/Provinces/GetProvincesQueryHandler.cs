using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Provinces;

internal sealed class GetProvincesQueryHandler : IRequestHandler<GetProvincesQuery, Result<IReadOnlyList<ProvinceListItem>>>
{
    private readonly ILookUpCatalogReader _reader;

    public GetProvincesQueryHandler(ILookUpCatalogReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<IReadOnlyList<ProvinceListItem>>> Handle(GetProvincesQuery request, CancellationToken ct)
    {
        var provinces = await _reader.GetProvincesAsync(ct);
        return Result<IReadOnlyList<ProvinceListItem>>.Success(provinces);
    }
}
