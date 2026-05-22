using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// <see cref="GetIncomeStatementQuery"/> handler — STUB.
/// Gerçek Dapper implementasyonu Faz 6'da gelecek.
/// </summary>
public sealed class GetIncomeStatementQueryHandler
    : IRequestHandler<GetIncomeStatementQuery, Result<IncomeStatementReport>>
{
    private readonly IAccountingReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetIncomeStatementQueryHandler(IAccountingReader reader)
        => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<IncomeStatementReport>> Handle(
        GetIncomeStatementQuery query,
        CancellationToken cancellationToken)
        => Result<IncomeStatementReport>.Success(
            await _reader.GetIncomeStatementAsync(query.CompanyId, query.From, query.To, cancellationToken));
}
