using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Blocks;

/// <summary>
/// <see cref="CreateBlockCommand"/> validation kuralları.
/// </summary>
public sealed class CreateBlockCommandValidator : AbstractValidator<CreateBlockCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public CreateBlockCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Site kimliği zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ada adı zorunludur.")
            .MaximumLength(100).WithMessage("Ada adı en fazla 100 karakter olabilir.");
    }
}
