using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.Budgets;

/// <summary>
/// <see cref="SetBudgetCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class SetBudgetCommandValidator : AbstractValidator<SetBudgetCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public SetBudgetCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "TenantId"].Value);

        RuleFor(x => x.AccountingPeriodId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "AccountingPeriodId"].Value);

        RuleFor(x => x.AccountCodeId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "AccountCodeId"].Value);

        RuleFor(x => x.BudgetedAmount)
            .GreaterThanOrEqualTo(0).WithMessage(_ => localizer["Validation.MustBeNonNegative", "BudgetedAmount"].Value);
    }
}
