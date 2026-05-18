using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Buildings;

/// <summary>
/// <see cref="UpdateBuildingCommand"/> validation kuralları.
/// </summary>
public sealed class UpdateBuildingCommandValidator : AbstractValidator<UpdateBuildingCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public UpdateBuildingCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Yapı adı zorunludur.")
            .MaximumLength(200).WithMessage("Yapı adı en fazla 200 karakter olabilir.");
        RuleFor(x => x.MunicipalNo).MaximumLength(50).When(x => x.MunicipalNo is not null);
        RuleFor(x => x.Type).IsInEnum().WithMessage("Geçersiz yapı tipi.");
    }
}
