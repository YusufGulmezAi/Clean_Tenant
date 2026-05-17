using FluentValidation;

namespace CleanTenant.Application.Features.Auth.TwoFactor.ConfirmTotpEnrollment;

/// <summary><see cref="ConfirmTotpEnrollmentCommand"/> validator'ı.</summary>
public sealed class ConfirmTotpEnrollmentCommandValidator : AbstractValidator<ConfirmTotpEnrollmentCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ConfirmTotpEnrollmentCommandValidator()
    {
        RuleFor(c => c.Code)
            .NotEmpty()
            .WithErrorCode("AUTH-2FA-004")
            .WithMessage("Doğrulama kodu zorunlu.");
    }
}
