using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <see cref="PublishBudgetCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class PublishBudgetCommandValidator : AbstractValidator<PublishBudgetCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public PublishBudgetCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.BudgetId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "BudgetId"].Value);

        RuleFor(x => x.ValidFrom)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "ValidFrom"].Value);
    }
}
