using FluentValidation;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="ResetUserPasswordCommand"/> FluentValidation doğrulaması.
/// </summary>
public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    /// <summary>Kural setini tanımlar.</summary>
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.UrlCode).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre boş olamaz.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
    }
}
