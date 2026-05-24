using MediatR;

namespace CleanTenant.Application.Features.Main.Parties.CurrentAccount;

/// <summary><see cref="GetUnitLedgerQuery"/> handler.</summary>
public sealed class GetUnitLedgerQueryHandler
    : IRequestHandler<GetUnitLedgerQuery, Result<IReadOnlyList<LedgerEntryRow>>>
{
    private readonly ICurrentAccountReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUnitLedgerQueryHandler(ICurrentAccountReader reader) => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LedgerEntryRow>>> Handle(
        GetUnitLedgerQuery request, CancellationToken cancellationToken)
    {
        var rows = await _reader.GetLedgerAsync(request.CompanyId, request.UnitId, cancellationToken);
        return Result<IReadOnlyList<LedgerEntryRow>>.Success(rows);
    }
}
