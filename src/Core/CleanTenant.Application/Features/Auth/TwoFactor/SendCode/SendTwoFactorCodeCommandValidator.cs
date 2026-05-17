using FluentValidation;

namespace CleanTenant.Application.Features.Auth.TwoFactor.SendCode;

/// <summary><see cref="SendTwoFactorCodeCommand"/> validator'ı.</summary>
public sealed class SendTwoFactorCodeCommandValidator : AbstractValidator<SendTwoFactorCodeCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public SendTwoFactorCodeCommandValidator()
    {
        RuleFor(c => c.ChallengeToken)
            .NotEqual(Guid.Empty)
            .WithErrorCode("AUTH-2FA-002")
            .WithMessage("ChallengeToken zorunlu.");

        RuleFor(c => c.Method)
            .NotEmpty()
            .WithErrorCode("AUTH-2FA-002")
            .WithMessage("Method zorunlu (Email veya Phone).");
    }
}
