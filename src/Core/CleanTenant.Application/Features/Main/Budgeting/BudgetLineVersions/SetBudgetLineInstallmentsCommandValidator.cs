using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary><see cref="SetBudgetLineInstallmentsCommand"/> FluentValidation kuralları.</summary>
public sealed class SetBudgetLineInstallmentsCommandValidator : AbstractValidator<SetBudgetLineInstallmentsCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public SetBudgetLineInstallmentsCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BudgetLineVersionId).NotEmpty();
        RuleForEach(x => x.Installments).ChildRules(i =>
        {
            i.RuleFor(x => x.Month).InclusiveBetween(1, 12);
            i.RuleFor(x => x.Amount).GreaterThanOrEqualTo(0m);
        });
    }
}
