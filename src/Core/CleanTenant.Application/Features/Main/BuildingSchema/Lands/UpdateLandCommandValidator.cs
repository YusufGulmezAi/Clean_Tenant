using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Lands;

/// <summary>
/// <see cref="UpdateLandCommand"/> validation kuralları.
/// </summary>
public sealed class UpdateLandCommandValidator : AbstractValidator<UpdateLandCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public UpdateLandCommandValidator()
    {
        RuleFor(x => x.LandId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ada adı zorunludur.")
            .MaximumLength(100).WithMessage("Ada adı en fazla 100 karakter olabilir.");
    }
}
