using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// <see cref="CreateAccountCodeCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class CreateAccountCodeCommandValidator : AbstractValidator<CreateAccountCodeCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateAccountCodeCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "TenantId"].Value);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(_ => localizer["Validation.AccountCode.Code.Required"].Value)
            .MaximumLength(20).WithMessage(_ => localizer["Validation.AccountCode.Code.MaxLength", 20].Value)
            .Matches(@"^\d{3}(\.\d{2}(\.\d{3})?)?$")
                .WithMessage(_ => localizer["Validation.AccountCode.Code.Format"].Value);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.AccountCode.Name.Required"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.AccountCode.Name.MaxLength", 256].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1024)
                .When(x => !string.IsNullOrWhiteSpace(x.Description), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.ParentCode)
            .Matches(@"^\d{3}(\.\d{2})?$")
                .When(x => !string.IsNullOrWhiteSpace(x.ParentCode), ApplyConditionTo.CurrentValidator)
                .WithMessage(_ => localizer["Validation.AccountCode.ParentCode.Format"].Value);
    }
}
