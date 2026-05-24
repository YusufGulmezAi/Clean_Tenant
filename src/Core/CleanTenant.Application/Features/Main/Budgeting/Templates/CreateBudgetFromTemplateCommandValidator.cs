using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary><see cref="CreateBudgetFromTemplateCommand"/> FluentValidation kuralları.</summary>
public sealed class CreateBudgetFromTemplateCommandValidator : AbstractValidator<CreateBudgetFromTemplateCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBudgetFromTemplateCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.FiscalYearId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "Title"].Value)
            .MaximumLength(120).WithMessage(_ => localizer["Validation.MaxLength", "Title", 120].Value);

        RuleFor(x => x.PeriodStartMonth!.Value).InclusiveBetween(1, 12).When(x => x.PeriodStartMonth.HasValue);
        RuleFor(x => x.PeriodEndMonth!.Value).InclusiveBetween(1, 12).When(x => x.PeriodEndMonth.HasValue);

        RuleFor(x => x)
            .Must(c => c.PeriodStartYear.HasValue == c.PeriodStartMonth.HasValue)
            .WithMessage(_ => "Başlangıç yıl ve ayı birlikte verilmelidir.")
            .Must(c => c.PeriodEndYear.HasValue == c.PeriodEndMonth.HasValue)
            .WithMessage(_ => "Bitiş yıl ve ayı birlikte verilmelidir.");
    }
}
