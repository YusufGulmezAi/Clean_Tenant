using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// <see cref="GetCostCenterReportQuery"/> handler — STUB.
/// Gerçek Dapper implementasyonu Faz 6'da gelecek.
/// </summary>
public sealed class GetCostCenterReportQueryHandler
    : IRequestHandler<GetCostCenterReportQuery, Result<IReadOnlyList<CostCenterReportEntry>>>
{
    private readonly IAccountingReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetCostCenterReportQueryHandler(IAccountingReader reader)
        => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CostCenterReportEntry>>> Handle(
        GetCostCenterReportQuery query,
        CancellationToken cancellationToken)
        => Result<IReadOnlyList<CostCenterReportEntry>>.Success(
            await _reader.GetCostCenterReportAsync(query.CompanyId, query.CostCenterId, query.From, query.To, cancellationToken));
}
