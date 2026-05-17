using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;

namespace CleanTenant.Application.Features.Auth.Logout;

/// <summary>
/// <para>
/// Logout akışı:
/// </para>
/// <list type="number">
///   <item>Mevcut session'ı Redis'ten sil.</item>
///   <item>Aynı context'in refresh token zincirini DB'de revoke et.</item>
/// </list>
/// <para>
/// Bearer token zorunlu (endpoint <c>[Authorize]</c>); session lookup
/// middleware geçtikten sonra çalışır.
/// </para>
/// </summary>
public sealed class LogoutCommandHandler
{
    private readonly IAuthSessionStore _sessionStore;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LogoutCommandHandler(
        IAuthSessionStore sessionStore,
        IRefreshTokenService refreshTokenService,
        ICurrentSessionAccessor sessionAccessor)
    {
        _sessionStore = sessionStore;
        _refreshTokenService = refreshTokenService;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Logout isteğini işler.</summary>
    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var current = _sessionAccessor.Current;
        if (current is null)
        {
            return Result.Failure(
                Error.Unauthorized("AUTH-008", "Mevcut oturum bulunamadı."));
        }

        await _sessionStore.DeleteAsync(current.SessionId, current.UserId, cancellationToken);
        await _refreshTokenService.RevokeChainAsync(
            current.UserId, current.ContextId, "UserLogout", cancellationToken);

        return Result.Success();
    }
}
