using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// <see cref="GetTrialBalanceQuery"/> handler — STUB.
/// Gerçek Dapper implementasyonu Faz 6'da gelecek.
/// </summary>
public sealed class GetTrialBalanceQueryHandler
    : IRequestHandler<GetTrialBalanceQuery, Result<TrialBalanceReport>>
{
    private readonly IAccountingReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetTrialBalanceQueryHandler(IAccountingReader reader)
        => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<TrialBalanceReport>> Handle(
        GetTrialBalanceQuery query,
        CancellationToken cancellationToken)
        => Result<TrialBalanceReport>.Success(
            await _reader.GetTrialBalanceAsync(query.CompanyId, query.FiscalYearId, query.Month, cancellationToken));
}
