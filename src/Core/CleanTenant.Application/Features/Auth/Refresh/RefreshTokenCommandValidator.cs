using FluentValidation;

namespace CleanTenant.Application.Features.Auth.Refresh;

/// <summary><see cref="RefreshTokenCommand"/> validator'ı.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken)
            .NotEmpty()
            .WithErrorCode("AUTH-005")
            .WithMessage("Refresh token zorunlu.");
    }
}
