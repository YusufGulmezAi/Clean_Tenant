using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// <see cref="UpdateBlockCommand"/> validator.
/// </summary>
public sealed class UpdateBlockCommandValidator : AbstractValidator<UpdateBlockCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public UpdateBlockCommandValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Blok adı zorunludur.")
            .MaximumLength(100).WithMessage("Blok adı en fazla 100 karakter olabilir.");
    }
}
