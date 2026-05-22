using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <see cref="CreateBudgetCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBudgetCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "TenantId"].Value);

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.FiscalYearId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "FiscalYearId"].Value);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "Title"].Value)
            .MaximumLength(120).WithMessage(_ => localizer["Validation.MaxLength", "Title", 120].Value);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage(_ => localizer["Validation.MaxLength", "Notes", 2000].Value)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
