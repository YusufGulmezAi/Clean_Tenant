using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>
/// <see cref="CreateCostCenterCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class CreateCostCenterCommandValidator : AbstractValidator<CreateCostCenterCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateCostCenterCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(_ => localizer["Validation.CostCenter.Code.Required"].Value)
            .MaximumLength(20).WithMessage(_ => localizer["Validation.CostCenter.Code.MaxLength", 20].Value);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.CostCenter.Name.Required"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.CostCenter.Name.MaxLength", 256].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1024)
                .When(x => !string.IsNullOrWhiteSpace(x.Description), ApplyConditionTo.CurrentValidator);
    }
}
