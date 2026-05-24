using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary><see cref="UpdateBudgetCommand"/> FluentValidation kuralları.</summary>
public sealed class UpdateBudgetCommandValidator : AbstractValidator<UpdateBudgetCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateBudgetCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BudgetId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "Title"].Value)
            .MaximumLength(120).WithMessage(_ => localizer["Validation.MaxLength", "Title", 120].Value);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Notes));
        RuleFor(x => x.PeriodStartMonth!.Value).InclusiveBetween(1, 12).When(x => x.PeriodStartMonth.HasValue);
        RuleFor(x => x.PeriodEndMonth!.Value).InclusiveBetween(1, 12).When(x => x.PeriodEndMonth.HasValue);
    }
}
