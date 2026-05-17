using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.VerifyTwoFactor;

/// <summary>
/// <para>
/// 2FA challenge'ı doğrular. Akış:
/// </para>
/// <list type="number">
///   <item>Challenge'ı Redis'ten oku — yoksa süresi dolmuş veya geçersiz token.</item>
///   <item>Kullanıcıyı yükle.</item>
///   <item>Method'a göre kod doğrula (recovery code ayrı API).</item>
///   <item>Challenge'ı sil (replay engelleme).</item>
///   <item><see cref="LoginFinalizer"/> ile TokenPair üret.</item>
/// </list>
/// </summary>
public sealed class VerifyTwoFactorCommandHandler
{
    /// <summary>Recovery code yöntemi adı — istemci bu string ile gönderir.</summary>
    public const string RecoveryCodeMethod = "RecoveryCode";

    private readonly UserManager<User> _userManager;
    private readonly ITwoFactorChallengeStore _challengeStore;
    private readonly LoginFinalizer _finalizer;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public VerifyTwoFactorCommandHandler(
        UserManager<User> userManager,
        ITwoFactorChallengeStore challengeStore,
        LoginFinalizer finalizer)
    {
        _userManager = userManager;
        _challengeStore = challengeStore;
        _finalizer = finalizer;
    }

    /// <summary>Verify isteğini işler.</summary>
    public async Task<Result<TokenPair>> HandleAsync(VerifyTwoFactorCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Method) || string.IsNullOrWhiteSpace(command.Code))
        {
            return Result<TokenPair>.Failure(
                Error.Validation("AUTH-2FA-001", "Yöntem ve kod zorunlu."));
        }

        var challenge = await _challengeStore.GetAsync(command.ChallengeToken, cancellationToken);
        if (challenge is null)
        {
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-2FA-CHALLENGE-NOT-FOUND",
                    "Challenge süresi doldu veya geçersiz. Lütfen tekrar login olun."));
        }

        var user = await _userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user is null)
        {
            await _challengeStore.DeleteAsync(command.ChallengeToken, cancellationToken);
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        var isValid = await ValidateCodeAsync(user, command.Method, command.Code, challenge);
        if (!isValid)
        {
            // Tekrar denemeye izin ver — challenge silinmez, ama lockout sayacını artır.
            await _userManager.AccessFailedAsync(user);
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-2FA-INVALID-CODE", "Doğrulama kodu hatalı veya süresi dolmuş."));
        }

        // Kod doğru — challenge'ı sil, finalize et.
        await _challengeStore.DeleteAsync(command.ChallengeToken, cancellationToken);
        await _userManager.ResetAccessFailedCountAsync(user);

        return await _finalizer.FinalizeAsync(
            user, challenge.Persona, challenge.ContextId, command.IpAddress, command.UserAgent, cancellationToken);
    }

    private async Task<bool> ValidateCodeAsync(User user, string method, string code, TwoFactorChallenge challenge)
    {
        if (string.Equals(method, RecoveryCodeMethod, StringComparison.OrdinalIgnoreCase))
        {
            var redeem = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);
            return redeem.Succeeded;
        }

        // Method, challenge.AvailableMethods içinde olmalı (challenge anındaki listeyle bağlı kal).
        if (!challenge.AvailableMethods.Any(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return await _userManager.VerifyTwoFactorTokenAsync(user, method, code);
    }
}
