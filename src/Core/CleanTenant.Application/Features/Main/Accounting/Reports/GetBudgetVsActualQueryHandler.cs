using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// <see cref="GetBudgetVsActualQuery"/> handler — STUB.
/// Gerçek Dapper implementasyonu Faz 6'da gelecek.
/// </summary>
public sealed class GetBudgetVsActualQueryHandler
    : IRequestHandler<GetBudgetVsActualQuery, Result<BudgetVsActualReport>>
{
    private readonly IAccountingReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBudgetVsActualQueryHandler(IAccountingReader reader)
        => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<BudgetVsActualReport>> Handle(
        GetBudgetVsActualQuery query,
        CancellationToken cancellationToken)
        => Result<BudgetVsActualReport>.Success(
            await _reader.GetBudgetVsActualAsync(query.CompanyId, query.FiscalYearId, query.Month, cancellationToken));
}
