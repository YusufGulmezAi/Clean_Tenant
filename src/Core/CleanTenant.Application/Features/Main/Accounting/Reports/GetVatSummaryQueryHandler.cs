using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// <see cref="GetVatSummaryQuery"/> handler — STUB.
/// Gerçek Dapper implementasyonu Faz 6'da gelecek.
/// </summary>
public sealed class GetVatSummaryQueryHandler
    : IRequestHandler<GetVatSummaryQuery, Result<VatSummaryReport>>
{
    private readonly IAccountingReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetVatSummaryQueryHandler(IAccountingReader reader)
        => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<VatSummaryReport>> Handle(
        GetVatSummaryQuery query,
        CancellationToken cancellationToken)
        => Result<VatSummaryReport>.Success(
            await _reader.GetVatSummaryAsync(query.CompanyId, query.Year, query.Month, cancellationToken));
}
