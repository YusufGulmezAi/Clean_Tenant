using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.EnrollTotp;

/// <summary>
/// TOTP enrollment'ı başlatır. <see cref="UserManager{TUser}.ResetAuthenticatorKeyAsync"/>
/// ile yeni bir Base32 secret üretip kullanıcıya döner. Bu adımda
/// <c>User.TwoFactorEnabled</c> değişmez — Confirm akışıyla onaylanır.
/// </summary>
public sealed class EnrollTotpCommandHandler
{
    private const string Issuer = "CleanTenant";

    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public EnrollTotpCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Enrollment isteğini işler.</summary>
    public async Task<Result<EnrollTotpResult>> HandleAsync(EnrollTotpCommand command, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result<EnrollTotpResult>.Failure(
                Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        await _userManager.ResetAuthenticatorKeyAsync(user);
        var secret = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Result<EnrollTotpResult>.Failure(
                Error.Failure("AUTH-2FA-SECRET-GEN-FAILED", "TOTP secret üretilemedi."));
        }

        var emailLabel = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id.ToString());
        var qrUri = $"otpauth://totp/{Issuer}:{emailLabel}?secret={secret}&issuer={Issuer}&digits=6&period=30";

        return Result<EnrollTotpResult>.Success(new EnrollTotpResult(secret, qrUri));
    }
}
