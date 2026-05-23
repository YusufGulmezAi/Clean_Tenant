using CleanTenant.Application.Common.Auth;
using FluentValidation;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PhoneMethod;

/// <summary>Telefon numarasının geçerli bir TR cep numarası olduğunu doğrular.</summary>
public sealed class SendPhoneVerificationCodeCommandValidator : AbstractValidator<SendPhoneVerificationCodeCommand>
{
    /// <summary>Doğrulama kurallarını tanımlar.</summary>
    public SendPhoneVerificationCodeCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithErrorCode("AUTH-2FA-PHONE-REQUIRED")
            .WithMessage("Telefon numarası zorunlu.")
            .Must(phone => LoginIdentifier.TryNormalizePhone(phone, out _))
            .WithErrorCode("AUTH-2FA-PHONE-INVALID")
            .WithMessage("Geçerli bir Türkiye cep telefonu girin.");
    }
}
