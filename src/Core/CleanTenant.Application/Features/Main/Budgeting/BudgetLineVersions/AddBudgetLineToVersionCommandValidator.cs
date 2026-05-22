using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary><see cref="AddBudgetLineToVersionCommand"/> FluentValidation kuralları.</summary>
public sealed class AddBudgetLineToVersionCommandValidator : AbstractValidator<AddBudgetLineToVersionCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddBudgetLineToVersionCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BudgetVersionId).NotEmpty();
        RuleFor(x => x.BudgetLineId).NotEmpty();
        RuleFor(x => x.PlannedAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.DueDayOfMonth).InclusiveBetween(1, 31);
        RuleFor(x => x.DistributionConfig).MaximumLength(4000).When(x => !string.IsNullOrWhiteSpace(x.DistributionConfig));
    }
}
