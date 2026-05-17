using FluentValidation;

namespace CleanTenant.Application.Features.System.ElevateToWrite;

/// <summary><see cref="ElevateToWriteCommand"/> validator'ı.</summary>
public sealed class ElevateToWriteCommandValidator : AbstractValidator<ElevateToWriteCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ElevateToWriteCommandValidator()
    {
        RuleFor(c => c.Reason)
            .NotEmpty().WithErrorCode("SUP-005").WithMessage("Sebep zorunlu.")
            .MinimumLength(20).WithErrorCode("SUP-005").WithMessage("Sebep en az 20 karakter olmalı.");
    }
}
