using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Parcels;

/// <summary>
/// <see cref="CreateParcelCommand"/> validation kuralları.
/// </summary>
public sealed class CreateParcelCommandValidator : AbstractValidator<CreateParcelCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public CreateParcelCommandValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Parsel adı zorunludur.")
            .MaximumLength(100).WithMessage("Parsel adı en fazla 100 karakter olabilir.");
    }
}
