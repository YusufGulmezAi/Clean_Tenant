using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// <see cref="CreateBlockCommand"/> validator.
/// </summary>
public sealed class CreateBlockCommandValidator : AbstractValidator<CreateBlockCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public CreateBlockCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Blok adı zorunludur.")
            .MaximumLength(100).WithMessage("Blok adı en fazla 100 karakter olabilir.");
    }
}
