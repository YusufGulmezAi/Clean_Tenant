using FluentValidation;

namespace CleanTenant.Application.Features.Auth.TwoFactor.VerifyTwoFactor;

/// <summary><see cref="VerifyTwoFactorCommand"/> validator'ı.</summary>
public sealed class VerifyTwoFactorCommandValidator : AbstractValidator<VerifyTwoFactorCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public VerifyTwoFactorCommandValidator()
    {
        RuleFor(c => c.ChallengeToken)
            .NotEqual(Guid.Empty)
            .WithErrorCode("AUTH-2FA-001")
            .WithMessage("ChallengeToken zorunlu.");

        RuleFor(c => c.Method)
            .NotEmpty()
            .WithErrorCode("AUTH-2FA-001")
            .WithMessage("Method zorunlu.");

        RuleFor(c => c.Code)
            .NotEmpty()
            .WithErrorCode("AUTH-2FA-001")
            .WithMessage("Doğrulama kodu zorunlu.");
    }
}
