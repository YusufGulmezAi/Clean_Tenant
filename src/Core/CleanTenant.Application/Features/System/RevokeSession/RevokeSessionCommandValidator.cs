using FluentValidation;

namespace CleanTenant.Application.Features.System.RevokeSession;

/// <summary><see cref="RevokeSessionCommand"/> validator'ı.</summary>
public sealed class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public RevokeSessionCommandValidator()
    {
        RuleFor(c => c.SessionId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("AUTH-014")
            .WithMessage("SessionId zorunlu.");

        RuleFor(c => c.Reason)
            .NotEmpty().WithErrorCode("AUTH-014").WithMessage("Sebep zorunlu.")
            .MinimumLength(20).WithErrorCode("AUTH-014").WithMessage("Sebep en az 20 karakter olmalı.");
    }
}
