using CleanTenant.SharedKernel.Time;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties.CurrentAccount;

/// <summary><see cref="GetUnitCurrentAccountKpiQuery"/> handler.</summary>
public sealed class GetUnitCurrentAccountKpiQueryHandler
    : IRequestHandler<GetUnitCurrentAccountKpiQuery, Result<CurrentAccountKpi>>
{
    private readonly ICurrentAccountReader _reader;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUnitCurrentAccountKpiQueryHandler(ICurrentAccountReader reader, IClock clock)
    {
        _reader = reader;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<CurrentAccountKpi>> Handle(
        GetUnitCurrentAccountKpiQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var kpi = await _reader.GetKpiAsync(request.CompanyId, request.UnitId, today, cancellationToken);
        return Result<CurrentAccountKpi>.Success(kpi);
    }
}
