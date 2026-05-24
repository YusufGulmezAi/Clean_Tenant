using CleanTenant.SharedKernel.Time;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties.CurrentAccount;

/// <summary><see cref="GetUnitsOverviewQuery"/> handler.</summary>
public sealed class GetUnitsOverviewQueryHandler
    : IRequestHandler<GetUnitsOverviewQuery, Result<IReadOnlyList<UnitOverviewRow>>>
{
    private readonly ICurrentAccountReader _reader;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUnitsOverviewQueryHandler(ICurrentAccountReader reader, IClock clock)
    {
        _reader = reader;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UnitOverviewRow>>> Handle(
        GetUnitsOverviewQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var rows = await _reader.GetUnitsOverviewAsync(request.CompanyId, today, cancellationToken);
        return Result<IReadOnlyList<UnitOverviewRow>>.Success(rows);
    }
}
