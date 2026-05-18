using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Parcels;

/// <summary>
/// <see cref="UpdateParcelCommand"/> validation kuralları.
/// </summary>
public sealed class UpdateParcelCommandValidator : AbstractValidator<UpdateParcelCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public UpdateParcelCommandValidator()
    {
        RuleFor(x => x.ParcelId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Parsel adı zorunludur.")
            .MaximumLength(100).WithMessage("Parsel adı en fazla 100 karakter olabilir.");
    }
}
