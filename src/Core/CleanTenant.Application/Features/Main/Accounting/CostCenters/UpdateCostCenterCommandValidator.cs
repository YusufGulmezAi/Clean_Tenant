using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>
/// <see cref="UpdateCostCenterCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class UpdateCostCenterCommandValidator : AbstractValidator<UpdateCostCenterCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateCostCenterCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CostCenterId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CostCenterId"].Value);

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.CostCenter.Name.Required"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.CostCenter.Name.MaxLength", 256].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1024)
                .When(x => !string.IsNullOrWhiteSpace(x.Description), ApplyConditionTo.CurrentValidator);
    }
}
