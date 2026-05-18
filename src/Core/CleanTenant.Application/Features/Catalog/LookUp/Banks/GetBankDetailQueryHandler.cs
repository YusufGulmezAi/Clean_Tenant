using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Banks;

internal sealed class GetBankDetailQueryHandler : IRequestHandler<GetBankDetailQuery, Result<BankDetail?>>
{
    private readonly ILookUpCatalogReader _reader;

    public GetBankDetailQueryHandler(ILookUpCatalogReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<BankDetail?>> Handle(GetBankDetailQuery request, CancellationToken ct)
    {
        var bankDetail = await _reader.GetBankDetailAsync(request.Id, ct);
        return Result<BankDetail?>.Success(bankDetail);
    }
}
