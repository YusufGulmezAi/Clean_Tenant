using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// <see cref="GetAccountStatementQuery"/> handler — STUB.
/// Gerçek Dapper implementasyonu Faz 6'da gelecek.
/// </summary>
public sealed class GetAccountStatementQueryHandler
    : IRequestHandler<GetAccountStatementQuery, Result<IReadOnlyList<AccountStatementEntry>>>
{
    private readonly IAccountingReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetAccountStatementQueryHandler(IAccountingReader reader)
        => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AccountStatementEntry>>> Handle(
        GetAccountStatementQuery query,
        CancellationToken cancellationToken)
        => Result<IReadOnlyList<AccountStatementEntry>>.Success(
            await _reader.GetAccountStatementAsync(query.CompanyId, query.AccountCode, query.From, query.To, cancellationToken));
}
