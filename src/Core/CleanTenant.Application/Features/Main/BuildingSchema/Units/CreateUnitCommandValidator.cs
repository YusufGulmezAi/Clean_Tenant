using FluentValidation;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Units;

/// <summary>
/// <see cref="CreateUnitCommand"/> validation kuralları.
/// </summary>
public sealed class CreateUnitCommandValidator : AbstractValidator<CreateUnitCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public CreateUnitCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("BB numarası zorunludur.")
            .MaximumLength(20).WithMessage("BB numarası en fazla 20 karakter olabilir.");
        RuleFor(x => x.NationalAddressCode).MaximumLength(50).When(x => x.NationalAddressCode is not null);
        RuleFor(x => x.Type).IsInEnum().WithMessage("Geçersiz BB tipi.");
        RuleFor(x => x.SquareMeters).GreaterThan(0).WithMessage("Metrekare 0'dan büyük olmalıdır.");
        RuleFor(x => x.LandShare).GreaterThanOrEqualTo(0).WithMessage("Arsa payı 0 veya daha büyük olmalıdır.");
        RuleFor(x => x.AllocatedArea).GreaterThan(0).When(x => x.AllocatedArea.HasValue).WithMessage("Tahsis alanı 0'dan büyük olmalıdır.");
        RuleFor(x => x.Orientation).IsInEnum().WithMessage("Geçersiz yön.");
        RuleFor(x => x.Layout).IsInEnum().WithMessage("Geçersiz oda/salon tipi.");
    }
}
