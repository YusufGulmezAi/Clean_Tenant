using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// <see cref="GetCashBookQuery"/> handler — STUB.
/// Gerçek Dapper implementasyonu Faz 6'da gelecek.
/// </summary>
public sealed class GetCashBookQueryHandler
    : IRequestHandler<GetCashBookQuery, Result<IReadOnlyList<CashBookEntry>>>
{
    private readonly IAccountingReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetCashBookQueryHandler(IAccountingReader reader)
        => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CashBookEntry>>> Handle(
        GetCashBookQuery query,
        CancellationToken cancellationToken)
        => Result<IReadOnlyList<CashBookEntry>>.Success(
            await _reader.GetCashBookAsync(query.CompanyId, query.AccountCode, query.From, query.To, cancellationToken));
}
