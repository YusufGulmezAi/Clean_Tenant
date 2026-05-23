using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Notifications;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PhoneMethod;

/// <summary>Telefon doğrulama kodu üretip SMS ile gönderir.</summary>
public sealed class SendPhoneVerificationCodeCommandHandler
    : IRequestHandler<SendPhoneVerificationCodeCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly ISmsSender _smsSender;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SendPhoneVerificationCodeCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor,
        ISmsSender smsSender)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
        _smsSender = smsSender;
    }

    /// <summary>İsteği işler.</summary>
    public async Task<Result> Handle(SendPhoneVerificationCodeCommand command, CancellationToken cancellationToken)
    {
        if (!LoginIdentifier.TryNormalizePhone(command.Phone, out var phone))
        {
            return Result.Failure(
                Error.Validation("AUTH-2FA-PHONE-INVALID", "Geçerli bir Türkiye cep telefonu girin."));
        }

        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, phone);
        await _smsSender.SendAsync(
            phone,
            $"CleanTenant doğrulama kodunuz: {token}",
            cancellationToken);

        return Result.Success();
    }
}
