using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Notifications;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.EmailMethod;

/// <summary>E-posta doğrulama kodu üretip kullanıcının e-postasına gönderir.</summary>
public sealed class SendEmailVerificationCodeCommandHandler
    : IRequestHandler<SendEmailVerificationCodeCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IEmailSender _emailSender;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SendEmailVerificationCodeCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
        _emailSender = emailSender;
    }

    /// <summary>İsteği işler.</summary>
    public async Task<Result> Handle(SendEmailVerificationCodeCommand command, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Failure(
                Error.Validation("AUTH-2FA-EMAIL-MISSING", "Hesabınızda kayıtlı bir e-posta adresi yok."));
        }

        var code = await _userManager.GenerateTwoFactorTokenAsync(user, TwoFactorDefaults.EmailMethod);
        await _emailSender.SendAsync(
            user.Email,
            "CleanTenant — E-posta doğrulama kodu",
            $"E-postanızı 2FA yöntemi olarak doğrulamak için kodunuz: {code}. Kod 5 dakika geçerlidir.",
            cancellationToken);

        return Result.Success();
    }
}
