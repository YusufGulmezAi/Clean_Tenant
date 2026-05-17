using FluentValidation;

namespace CleanTenant.Application.Features.System.EnterSupportMode;

/// <summary><see cref="EnterSupportModeCommand"/> validator'ı.</summary>
public sealed class EnterSupportModeCommandValidator : AbstractValidator<EnterSupportModeCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public EnterSupportModeCommandValidator()
    {
        RuleFor(c => c.TargetTenantId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("SUP-001")
            .WithMessage("Hedef tenant id zorunlu.");

        RuleFor(c => c.Reason)
            .NotEmpty().WithErrorCode("SUP-001").WithMessage("Sebep zorunlu.")
            .MinimumLength(20).WithErrorCode("SUP-001").WithMessage("Sebep en az 20 karakter olmalı.");
    }
}
