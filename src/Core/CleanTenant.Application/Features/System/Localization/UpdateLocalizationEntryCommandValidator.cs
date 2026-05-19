using FluentValidation;

namespace CleanTenant.Application.Features.System.Localization;

/// <summary><see cref="UpdateLocalizationEntryCommand"/> validation kuralları.</summary>
public sealed class UpdateLocalizationEntryCommandValidator
    : AbstractValidator<UpdateLocalizationEntryCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public UpdateLocalizationEntryCommandValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Anahtar zorunlu.")
            .MaximumLength(256);

        RuleFor(x => x.Culture)
            .NotEmpty().WithMessage("Kültür kodu zorunlu.")
            .MaximumLength(16);

        RuleFor(x => x.NewValue)
            .NotNull().WithMessage("Değer null olamaz.")
            .MaximumLength(4000);
    }
}
