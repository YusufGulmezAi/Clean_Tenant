using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Banks;

internal sealed class GetBanksQueryHandler : IRequestHandler<GetBanksQuery, Result<IReadOnlyList<BankListItem>>>
{
    private readonly ILookUpCatalogReader _reader;

    public GetBanksQueryHandler(ILookUpCatalogReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<IReadOnlyList<BankListItem>>> Handle(GetBanksQuery request, CancellationToken ct)
    {
        var banks = await _reader.GetBanksAsync(ct);
        return Result<IReadOnlyList<BankListItem>>.Success(banks);
    }
}
