using CleanTenant.Application.Common.Auth;
using FluentValidation;

namespace CleanTenant.Application.Features.CompanyUsers;

/// <summary>
/// <see cref="CreateCompanyUserCommand"/> FluentValidation doğrulaması.
/// </summary>
public sealed class CreateCompanyUserCommandValidator : AbstractValidator<CreateCompanyUserCommand>
{
    /// <summary>Kural setini tanımlar.</summary>
    public CreateCompanyUserCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Yönetim kimliği zorunludur.");

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Site kimliği zorunludur.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad boş olamaz.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad boş olamaz.")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.");

        RuleFor(x => x.PhoneNumber)
            .Must(phone => LoginIdentifier.TryNormalizePhone(phone!, out _))
            .WithMessage("Geçerli bir Türkiye cep telefonu girin (örn: 05321234567 veya +90 532 123 45 67).")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");

        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("En az bir rol seçilmelidir.");
    }
}
