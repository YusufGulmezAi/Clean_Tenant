using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;

/// <summary><see cref="GenerateBudgetAccrualCommand"/> FluentValidation kuralları.</summary>
public sealed class GenerateBudgetAccrualCommandValidator : AbstractValidator<GenerateBudgetAccrualCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public GenerateBudgetAccrualCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BudgetId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
