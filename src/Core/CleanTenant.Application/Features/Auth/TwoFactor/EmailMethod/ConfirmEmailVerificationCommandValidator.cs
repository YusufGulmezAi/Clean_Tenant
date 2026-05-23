using FluentValidation;

namespace CleanTenant.Application.Features.Auth.TwoFactor.EmailMethod;

/// <summary>Doğrulama kodunun girilmiş olduğunu kontrol eder.</summary>
public sealed class ConfirmEmailVerificationCommandValidator : AbstractValidator<ConfirmEmailVerificationCommand>
{
    /// <summary>Doğrulama kurallarını tanımlar.</summary>
    public ConfirmEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithErrorCode("AUTH-2FA-CODE-REQUIRED")
            .WithMessage("Doğrulama kodu zorunlu.");
    }
}
