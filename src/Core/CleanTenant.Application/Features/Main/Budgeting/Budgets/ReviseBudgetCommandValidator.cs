using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary><see cref="ReviseBudgetCommand"/> FluentValidation kuralları.</summary>
public sealed class ReviseBudgetCommandValidator : AbstractValidator<ReviseBudgetCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public ReviseBudgetCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BudgetId).NotEmpty();
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);

        RuleForEach(x => x.LineOverrides!).ChildRules(child =>
        {
            child.RuleFor(o => o.BudgetLineId).NotEmpty();
            child.RuleFor(o => o.NewPlannedAmount!.Value)
                .GreaterThanOrEqualTo(0m)
                .When(o => o.NewPlannedAmount.HasValue);
            child.RuleFor(o => o.NewDueDayOfMonth!.Value)
                .InclusiveBetween(1, 31)
                .When(o => o.NewDueDayOfMonth.HasValue);
        }).When(x => x.LineOverrides is { Count: > 0 });
    }
}
