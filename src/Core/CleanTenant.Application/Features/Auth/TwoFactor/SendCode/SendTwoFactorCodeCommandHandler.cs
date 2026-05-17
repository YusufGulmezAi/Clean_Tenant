using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Notifications;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.AspNetCore.Identity;

using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.SendCode;

/// <summary>
/// Login challenge sürecinde kullanıcının kayıtlı e-posta / telefon adresine
/// 2FA doğrulama kodu yollar. Provider TokenProvider'larından kod üretip
/// <see cref="IEmailSender"/> / <see cref="ISmsSender"/> ile gönderir.
/// </summary>
public sealed class SendTwoFactorCodeCommandHandler : IRequestHandler<SendTwoFactorCodeCommand, Result>
{
    private const string EmailMethod = "Email";
    private const string PhoneMethod = "Phone";

    private readonly UserManager<User> _userManager;
    private readonly ITwoFactorChallengeStore _challengeStore;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SendTwoFactorCodeCommandHandler(
        UserManager<User> userManager,
        ITwoFactorChallengeStore challengeStore,
        IEmailSender emailSender,
        ISmsSender smsSender)
    {
        _userManager = userManager;
        _challengeStore = challengeStore;
        _emailSender = emailSender;
        _smsSender = smsSender;
    }

    /// <summary>SendCode isteğini işler.</summary>
    public async Task<Result> Handle(SendTwoFactorCodeCommand command, CancellationToken cancellationToken)
    {
        var challenge = await _challengeStore.GetAsync(command.ChallengeToken, cancellationToken);
        if (challenge is null)
        {
            return Result.Failure(
                Error.Unauthorized("AUTH-2FA-CHALLENGE-NOT-FOUND",
                    "Challenge süresi doldu veya geçersiz."));
        }

        if (!challenge.AvailableMethods.Any(m => string.Equals(m, command.Method, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(
                Error.Forbidden("AUTH-2FA-METHOD-NOT-AVAILABLE",
                    "Bu yöntem hesabın aktif 2FA yöntemleri arasında değil."));
        }

        var user = await _userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.Unauthorized("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        if (string.Equals(command.Method, EmailMethod, StringComparison.OrdinalIgnoreCase))
        {
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, EmailMethod);
            await _emailSender.SendAsync(
                user.Email!,
                "CleanTenant doğrulama kodu",
                $"Doğrulama kodunuz: {code}. Kod 5 dakika geçerlidir.",
                cancellationToken);
            return Result.Success();
        }

        if (string.Equals(command.Method, PhoneMethod, StringComparison.OrdinalIgnoreCase))
        {
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, PhoneMethod);
            await _smsSender.SendAsync(
                user.PhoneNumber!,
                $"CleanTenant doğrulama kodunuz: {code}",
                cancellationToken);
            return Result.Success();
        }

        return Result.Failure(
            Error.Validation("AUTH-2FA-003", "Yöntem yalnız 'Email' veya 'Phone' olabilir (TOTP authenticator app'inden okunur)."));
    }
}
