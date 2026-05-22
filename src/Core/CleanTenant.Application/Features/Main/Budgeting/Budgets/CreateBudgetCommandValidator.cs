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

        // Period ayları verilirse 1-12 aralığında olmalı
        RuleFor(x => x.PeriodStartMonth!.Value)
            .InclusiveBetween(1, 12).When(x => x.PeriodStartMonth.HasValue);
        RuleFor(x => x.PeriodEndMonth!.Value)
            .InclusiveBetween(1, 12).When(x => x.PeriodEndMonth.HasValue);

        // Period kısmen verilmişse: start çifti birlikte, end çifti birlikte olmalı
        RuleFor(x => x)
            .Must(c => (c.PeriodStartYear.HasValue == c.PeriodStartMonth.HasValue))
            .WithMessage(_ => "Başlangıç yıl ve ayı birlikte verilmelidir.")
            .Must(c => (c.PeriodEndYear.HasValue == c.PeriodEndMonth.HasValue))
            .WithMessage(_ => "Bitiş yıl ve ayı birlikte verilmelidir.");
    }
}
