using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PreAuthEnrollment;

/// <summary>
/// Pre-auth enrollment'ı başlatır:
/// <list type="number">
///   <item>Challenge token Redis'te doğrulanır.</item>
///   <item>Kullanıcı bulunur.</item>
///   <item>Authenticator key (yoksa) <see cref="UserManager{TUser}.ResetAuthenticatorKeyAsync"/> ile üretilir; varsa mevcut korunur.</item>
///   <item>otpauth URI + secret döner; QR kod istemci tarafında üretilir.</item>
/// </list>
/// </summary>
public sealed class StartPreAuthEnrollmentQueryHandler
    : IRequestHandler<StartPreAuthEnrollmentQuery, Result<StartPreAuthEnrollmentResult>>
{
    private const string Issuer = "CleanTenant";

    private readonly IPreAuthEnrollmentStore _store;
    private readonly UserManager<User> _userManager;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public StartPreAuthEnrollmentQueryHandler(
        IPreAuthEnrollmentStore store,
        UserManager<User> userManager)
    {
        _store = store;
        _userManager = userManager;
    }

    /// <summary>Start isteğini işler.</summary>
    public async Task<Result<StartPreAuthEnrollmentResult>> Handle(
        StartPreAuthEnrollmentQuery query,
        CancellationToken cancellationToken)
    {
        var challenge = await _store.GetAsync(query.ChallengeToken, cancellationToken);
        if (challenge is null)
        {
            return Result<StartPreAuthEnrollmentResult>.Failure(
                Error.Unauthorized("AUTH-2FA-ENROLL-CHALLENGE-NOT-FOUND",
                    "Enrollment süresi doldu veya geçersiz token. Lütfen tekrar giriş yapın."));
        }

        var user = await _userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user is null)
        {
            await _store.DeleteAsync(query.ChallengeToken, cancellationToken);
            return Result<StartPreAuthEnrollmentResult>.Failure(
                Error.Unauthorized("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        // Authenticator key (yoksa) üret. Var olan key korunur — kullanıcı sayfayı
        // yenilerse aynı QR/secret döner, authenticator app'inden silmek zorunda kalmaz.
        var secret = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(secret))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            secret = await _userManager.GetAuthenticatorKeyAsync(user);
        }
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Result<StartPreAuthEnrollmentResult>.Failure(
                Error.Failure("AUTH-2FA-SECRET-GEN-FAILED", "TOTP secret üretilemedi."));
        }

        var emailLabel = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id.ToString());
        var qrUri = $"otpauth://totp/{Issuer}:{emailLabel}?secret={secret}&issuer={Issuer}&digits=6&period=30";

        return Result<StartPreAuthEnrollmentResult>.Success(
            new StartPreAuthEnrollmentResult(challenge.Email, secret, qrUri));
    }
}
