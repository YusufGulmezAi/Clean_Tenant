using FluentValidation;

namespace CleanTenant.Application.Features.Auth.Login;

/// <summary>
/// <see cref="LoginCommand"/> validator'ı. Inline validation'lar
/// (v0.1.5.a'dan beri handler içindeydi) v0.1.6'da MediatR pipeline'a taşındı.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public LoginCommandValidator()
    {
        RuleFor(c => c.Identifier)
            .NotEmpty()
            .WithErrorCode("AUTH-001")
            .WithMessage("Kullanıcı kimliği zorunlu.");

        RuleFor(c => c.Password)
            .NotEmpty()
            .WithErrorCode("AUTH-001")
            .WithMessage("Şifre zorunlu.");
    }
}
