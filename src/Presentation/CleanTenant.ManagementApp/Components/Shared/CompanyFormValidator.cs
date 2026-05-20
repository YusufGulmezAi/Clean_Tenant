using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// Site formu validasyon kuralları. Mode'a göre değişiklik gösterir
/// (Create'de tüm alanlar, Edit'te sadece düzenlenebilir alanlar).
/// v0.2.11.d — lokalize (DbStringLocalizer).
/// </summary>
public sealed class CompanyFormValidator : AbstractValidator<CompanyFormModel>
{
    /// <summary>Validatör'ü moda ve localizer'a göre başlatır.</summary>
    public CompanyFormValidator(CompanyFormMode mode, IStringLocalizer localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.Company.Name.Required"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.Company.Name.MaxLength", 256].Value);

        RuleFor(x => x.LegalName)
            .MaximumLength(512).WithMessage(_ => localizer["Validation.Company.LegalName.MaxLength", 512].Value);

        RuleFor(x => x.Vkn)
            .Matches(@"^[0-9]{10}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Vkn), ApplyConditionTo.CurrentValidator)
                .WithMessage(_ => localizer["Validation.Company.Vkn.Format"].Value);

        RuleFor(x => x.Email)
            .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.Email), ApplyConditionTo.CurrentValidator)
                .WithMessage(_ => localizer["Validation.Company.Email.Invalid"].Value)
            .MaximumLength(256);

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage(_ => localizer["Validation.Company.Phone.MaxLength", 20].Value);
    }
}
