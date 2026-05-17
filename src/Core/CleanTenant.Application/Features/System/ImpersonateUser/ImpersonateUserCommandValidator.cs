using FluentValidation;

namespace CleanTenant.Application.Features.System.ImpersonateUser;

/// <summary><see cref="ImpersonateUserCommand"/> validator'ı.</summary>
public sealed class ImpersonateUserCommandValidator : AbstractValidator<ImpersonateUserCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ImpersonateUserCommandValidator()
    {
        RuleFor(c => c.TargetUserUrlCode)
            .NotEmpty()
            .WithErrorCode("SUP-008")
            .WithMessage("Hedef kullanıcı URL kodu zorunlu.");

        RuleFor(c => c.Reason)
            .NotEmpty().WithErrorCode("SUP-008").WithMessage("Sebep zorunlu.")
            .MinimumLength(20).WithErrorCode("SUP-008").WithMessage("Sebep en az 20 karakter olmalı.");
    }
}
