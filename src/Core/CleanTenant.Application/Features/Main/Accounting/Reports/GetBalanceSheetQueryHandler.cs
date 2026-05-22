using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// <see cref="GetBalanceSheetQuery"/> handler — STUB.
/// Gerçek Dapper implementasyonu Faz 6'da gelecek.
/// </summary>
public sealed class GetBalanceSheetQueryHandler
    : IRequestHandler<GetBalanceSheetQuery, Result<BalanceSheetReport>>
{
    private readonly IAccountingReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBalanceSheetQueryHandler(IAccountingReader reader)
        => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<BalanceSheetReport>> Handle(
        GetBalanceSheetQuery query,
        CancellationToken cancellationToken)
        => Result<BalanceSheetReport>.Success(
            await _reader.GetBalanceSheetAsync(query.CompanyId, query.AsOf, cancellationToken));
}
