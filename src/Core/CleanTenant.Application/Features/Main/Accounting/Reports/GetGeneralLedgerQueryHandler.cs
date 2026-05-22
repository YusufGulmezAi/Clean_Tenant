using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// <see cref="GetGeneralLedgerQuery"/> handler — STUB.
/// Gerçek Dapper implementasyonu Faz 6'da gelecek.
/// </summary>
public sealed class GetGeneralLedgerQueryHandler
    : IRequestHandler<GetGeneralLedgerQuery, Result<IReadOnlyList<GeneralLedgerEntry>>>
{
    private readonly IAccountingReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetGeneralLedgerQueryHandler(IAccountingReader reader)
        => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GeneralLedgerEntry>>> Handle(
        GetGeneralLedgerQuery query,
        CancellationToken cancellationToken)
        => Result<IReadOnlyList<GeneralLedgerEntry>>.Success(
            await _reader.GetGeneralLedgerAsync(query.CompanyId, query.AccountCode, query.From, query.To, cancellationToken));
}
