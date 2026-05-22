using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.ExemptionRules;

/// <summary><see cref="CreateExemptionRuleCommand"/> FluentValidation kuralları.</summary>
public sealed class CreateExemptionRuleCommandValidator : AbstractValidator<CreateExemptionRuleCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateExemptionRuleCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.BudgetLineId).NotEmpty();
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
