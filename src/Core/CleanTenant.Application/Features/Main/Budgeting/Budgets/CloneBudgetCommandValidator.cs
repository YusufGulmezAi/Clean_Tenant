using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <see cref="CloneBudgetCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class CloneBudgetCommandValidator : AbstractValidator<CloneBudgetCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CloneBudgetCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "TenantId"].Value);

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.SourceBudgetId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "SourceBudgetId"].Value);

        RuleFor(x => x.NewFiscalYearId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "NewFiscalYearId"].Value);

        RuleFor(x => x.NewTitle)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "NewTitle"].Value)
            .MaximumLength(120).WithMessage(_ => localizer["Validation.MaxLength", "NewTitle", 120].Value);

        RuleFor(x => x.PeriodStartMonth!.Value)
            .InclusiveBetween(1, 12).When(x => x.PeriodStartMonth.HasValue);
        RuleFor(x => x.PeriodEndMonth!.Value)
            .InclusiveBetween(1, 12).When(x => x.PeriodEndMonth.HasValue);

        RuleFor(x => x)
            .Must(c => c.PeriodStartYear.HasValue == c.PeriodStartMonth.HasValue)
            .WithMessage(_ => "Başlangıç yıl ve ayı birlikte verilmelidir.")
            .Must(c => c.PeriodEndYear.HasValue == c.PeriodEndMonth.HasValue)
            .WithMessage(_ => "Bitiş yıl ve ayı birlikte verilmelidir.");
    }
}
