using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.ResidentialTypes;

internal sealed class GetResidentialTypesQueryHandler : IRequestHandler<GetResidentialTypesQuery, Result<IReadOnlyList<ResidentialTypeListItem>>>
{
    private readonly ILookUpCatalogReader _reader;

    public GetResidentialTypesQueryHandler(ILookUpCatalogReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<IReadOnlyList<ResidentialTypeListItem>>> Handle(GetResidentialTypesQuery request, CancellationToken ct)
    {
        var residentialTypes = await _reader.GetResidentialTypesAsync(ct);
        return Result<IReadOnlyList<ResidentialTypeListItem>>.Success(residentialTypes);
    }
}
