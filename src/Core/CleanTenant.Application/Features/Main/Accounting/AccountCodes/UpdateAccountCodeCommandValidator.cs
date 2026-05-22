using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// <see cref="UpdateAccountCodeCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class UpdateAccountCodeCommandValidator : AbstractValidator<UpdateAccountCodeCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateAccountCodeCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.AccountCodeId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "AccountCodeId"].Value);

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.AccountCode.Name.Required"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.AccountCode.Name.MaxLength", 256].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1024)
                .When(x => !string.IsNullOrWhiteSpace(x.Description), ApplyConditionTo.CurrentValidator);
    }
}
