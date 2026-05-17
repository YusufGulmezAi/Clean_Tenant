using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;

using MediatR;

namespace CleanTenant.Application.Features.System.RevokeSession;

/// <summary>
/// Belirtilen tek bir Redis session'ı revoke eder. Hedef session'ın
/// kullanıcısı bir sonraki istekte 401 alır.
/// </summary>
public sealed class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, Result>
{
    private readonly IAuthSessionStore _sessionStore;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RevokeSessionCommandHandler(IAuthSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    /// <summary>Tek session revoke uygular.</summary>
    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await _sessionStore.GetAsync(command.SessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure(
                Error.NotFound("AUTH-015", "Session bulunamadı veya zaten süresi dolmuş."));
        }

        await _sessionStore.DeleteAsync(command.SessionId, session.UserId, cancellationToken);
        return Result.Success();
    }
}
