using CleanTenant.Application.Common.Auth;
using FluentValidation;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="UpdateUserCommand"/> FluentValidation doğrulaması.
/// </summary>
public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    /// <summary>Kural setini tanımlar.</summary>
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UrlCode).NotEmpty();

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad boş olamaz.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad boş olamaz.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256);

        RuleFor(x => x.PhoneNumber)
            .Must(phone => LoginIdentifier.TryNormalizePhone(phone!, out _))
            .WithMessage("Geçerli bir Türkiye cep telefonu girin (örn: 05321234567 veya +90 532 123 45 67).")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("En az bir rol seçilmelidir.");
    }
}
