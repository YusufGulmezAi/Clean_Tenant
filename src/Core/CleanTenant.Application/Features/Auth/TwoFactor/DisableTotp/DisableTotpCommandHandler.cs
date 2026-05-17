using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.DisableTotp;

/// <summary>
/// TOTP yöntemini kapatır. Akış:
/// <list type="number">
///   <item>TOTP enrolled değilse — hata.</item>
///   <item>Diğer 2FA yöntemleri var → TOTP token'ı sil; <c>TwoFactorEnabled</c> kalır.</item>
///   <item>Diğer yöntem yok ve kullanıcı System scope'ta → <c>AUTH-2FA-LAST-METHOD-LOCK</c>.</item>
///   <item>Diğer yöntem yok ve System değil → TOTP sil + <c>TwoFactorEnabled=false</c>.</item>
/// </list>
/// </summary>
public sealed class DisableTotpCommandHandler
{
    private const string AuthenticatorProvider = "Authenticator";
    private const string AuthenticatorTokenLoginProvider = "[AspNetUserStore]";
    private const string AuthenticatorTokenName = "AuthenticatorKey";

    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly LoginFinalizer _finalizer;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DisableTotpCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor,
        LoginFinalizer finalizer)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
        _finalizer = finalizer;
    }

    /// <summary>Disable isteğini işler.</summary>
    public async Task<Result> HandleAsync(DisableTotpCommand command, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);
        if (!providers.Any(p => string.Equals(p, AuthenticatorProvider, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(
                Error.Failure("AUTH-2FA-TOTP-NOT-ENROLLED", "TOTP yöntemi aktif değil."));
        }

        var hasOtherMethods = providers.Any(p =>
            !string.Equals(p, AuthenticatorProvider, StringComparison.OrdinalIgnoreCase));

        if (!hasOtherMethods &&
            await _finalizer.HasSystemScopeAsync(user.Id, cancellationToken))
        {
            return Result.Failure(
                Error.Forbidden("AUTH-2FA-LAST-METHOD-LOCK",
                    "System hesabınız için son 2FA yöntemini kapatamazsınız."));
        }

        // AuthenticatorKey'i sil (null değer ile)
        await _userManager.SetAuthenticationTokenAsync(
            user, AuthenticatorTokenLoginProvider, AuthenticatorTokenName, null!);

        if (!hasOtherMethods)
        {
            // Geriye hiç yöntem kalmadı — TwoFactorEnabled=false
            await _userManager.SetTwoFactorEnabledAsync(user, false);
        }

        return Result.Success();
    }
}
