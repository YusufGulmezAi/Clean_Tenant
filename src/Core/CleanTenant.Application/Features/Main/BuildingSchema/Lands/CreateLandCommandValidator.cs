using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Lands;

/// <summary>
/// <see cref="CreateLandCommand"/> validation kuralları.
/// </summary>
public sealed class CreateLandCommandValidator : AbstractValidator<CreateLandCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public CreateLandCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Site kimliği zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ada adı zorunludur.")
            .MaximumLength(100).WithMessage("Ada adı en fazla 100 karakter olabilir.");
    }
}
