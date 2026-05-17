using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;

namespace CleanTenant.Application.Features.System.RevokeSession;

/// <summary>
/// Belirtilen tek bir Redis session'ı revoke eder. Hedef session'ın
/// kullanıcısı bir sonraki istekte 401 alır.
/// </summary>
public sealed class RevokeSessionCommandHandler
{
    private readonly IAuthSessionStore _sessionStore;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RevokeSessionCommandHandler(IAuthSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    /// <summary>Tek session revoke uygular.</summary>
    public async Task<Result> HandleAsync(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Length < 20)
        {
            return Result.Failure(
                Error.Validation("AUTH-014", "Sebep zorunlu (minimum 20 karakter)."));
        }

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
