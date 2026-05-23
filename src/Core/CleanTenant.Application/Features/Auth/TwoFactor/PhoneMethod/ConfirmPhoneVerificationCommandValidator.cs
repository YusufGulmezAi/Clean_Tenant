using CleanTenant.Application.Common.Auth;
using FluentValidation;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PhoneMethod;

/// <summary>Telefon numarası ve kodun girilmiş/geçerli olduğunu kontrol eder.</summary>
public sealed class ConfirmPhoneVerificationCommandValidator : AbstractValidator<ConfirmPhoneVerificationCommand>
{
    /// <summary>Doğrulama kurallarını tanımlar.</summary>
    public ConfirmPhoneVerificationCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithErrorCode("AUTH-2FA-PHONE-REQUIRED")
            .WithMessage("Telefon numarası zorunlu.")
            .Must(phone => LoginIdentifier.TryNormalizePhone(phone, out _))
            .WithErrorCode("AUTH-2FA-PHONE-INVALID")
            .WithMessage("Geçerli bir Türkiye cep telefonu girin.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithErrorCode("AUTH-2FA-CODE-REQUIRED")
            .WithMessage("Doğrulama kodu zorunlu.");
    }
}
