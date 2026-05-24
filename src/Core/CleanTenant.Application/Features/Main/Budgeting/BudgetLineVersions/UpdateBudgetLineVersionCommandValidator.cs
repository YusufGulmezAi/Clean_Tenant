using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary><see cref="UpdateBudgetLineVersionCommand"/> FluentValidation kuralları.</summary>
public sealed class UpdateBudgetLineVersionCommandValidator : AbstractValidator<UpdateBudgetLineVersionCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateBudgetLineVersionCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BudgetLineVersionId).NotEmpty();
        RuleFor(x => x.PlannedAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.DueDayOfMonth).InclusiveBetween(1, 31);
        RuleFor(x => x.InstallmentStartMonth!.Value).InclusiveBetween(1, 12).When(x => x.InstallmentStartMonth.HasValue);
        RuleFor(x => x.InstallmentEndMonth!.Value).InclusiveBetween(1, 12).When(x => x.InstallmentEndMonth.HasValue);
    }
}
