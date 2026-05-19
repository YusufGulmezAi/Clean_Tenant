using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Context;
using MediatR;

namespace CleanTenant.Application.Features.Auth.CurrentSession;

/// <summary>
/// <see cref="GetCurrentSessionInfoQuery"/> handler — aktif Redis session'dan
/// Blazor UI bileşenlerinin ihtiyaç duyduğu projection'ı üretir. Session
/// pipeline behavior'ı (SessionLoaderBehavior) tarafından doldurulur, dolayısıyla
/// SignalR scope'unda da çalışır.
/// </summary>
public sealed class GetCurrentSessionInfoQueryHandler : IRequestHandler<GetCurrentSessionInfoQuery, CurrentSessionInfo>
{
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılığını alır.</summary>
    public GetCurrentSessionInfoQueryHandler(ICurrentSessionAccessor sessionAccessor)
    {
        _sessionAccessor = sessionAccessor;
    }

    /// <inheritdoc />
    public Task<CurrentSessionInfo> Handle(GetCurrentSessionInfoQuery request, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current;
        if (session is null)
        {
            return Task.FromResult(new CurrentSessionInfo(false, false, []));
        }

        return Task.FromResult(new CurrentSessionInfo(
            IsAuthenticated: true,
            IsSystem: session.ScopeLevel == ScopeLevel.System,
            PermissionCodes: session.Permissions));
    }
}
