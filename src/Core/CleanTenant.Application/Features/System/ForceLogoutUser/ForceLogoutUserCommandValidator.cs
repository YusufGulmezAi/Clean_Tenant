using FluentValidation;

namespace CleanTenant.Application.Features.System.ForceLogoutUser;

/// <summary><see cref="ForceLogoutUserCommand"/> validator'ı.</summary>
public sealed class ForceLogoutUserCommandValidator : AbstractValidator<ForceLogoutUserCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ForceLogoutUserCommandValidator()
    {
        RuleFor(c => c.TargetUserUrlCode)
            .NotEmpty()
            .WithErrorCode("AUTH-012")
            .WithMessage("Hedef kullanıcı URL kodu zorunlu.");

        RuleFor(c => c.Reason)
            .NotEmpty().WithErrorCode("AUTH-012").WithMessage("Sebep zorunlu.")
            .MinimumLength(20).WithErrorCode("AUTH-012").WithMessage("Sebep en az 20 karakter olmalı.");
    }
}
