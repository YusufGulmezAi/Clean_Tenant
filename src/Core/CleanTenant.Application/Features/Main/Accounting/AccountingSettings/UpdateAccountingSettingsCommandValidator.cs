using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.AccountingSettings;

/// <summary>
/// <see cref="UpdateAccountingSettingsCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class UpdateAccountingSettingsCommandValidator : AbstractValidator<UpdateAccountingSettingsCommand>
{
    private static readonly string[] ValidCurrencies = ["TRY", "USD", "EUR", "GBP"];

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateAccountingSettingsCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.DefaultCurrency)
            .NotEmpty().WithMessage(_ => localizer["Validation.AccountingSettings.Currency.Required"].Value)
            .Must(c => ValidCurrencies.Contains(c))
                .WithMessage(_ => localizer["Validation.AccountingSettings.Currency.Invalid"].Value);
    }
}
