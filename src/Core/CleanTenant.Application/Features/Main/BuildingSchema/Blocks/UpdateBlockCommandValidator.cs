using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// <see cref="UpdateBlockCommand"/> validation kuralları.
/// </summary>
public sealed class UpdateBlockCommandValidator : AbstractValidator<UpdateBlockCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public UpdateBlockCommandValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ada adı zorunludur.")
            .MaximumLength(100).WithMessage("Ada adı en fazla 100 karakter olabilir.");
    }
}
