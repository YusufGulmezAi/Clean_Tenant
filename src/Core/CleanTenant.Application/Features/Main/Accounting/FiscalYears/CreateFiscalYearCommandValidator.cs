using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>
/// <see cref="CreateFiscalYearCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class CreateFiscalYearCommandValidator : AbstractValidator<CreateFiscalYearCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateFiscalYearCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage(_ => localizer["Validation.FiscalYear.Label.Required"].Value)
            .MaximumLength(50).WithMessage(_ => localizer["Validation.FiscalYear.Label.MaxLength", 50].Value);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage(_ => localizer["Validation.FiscalYear.StartDate.Required"].Value);

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage(_ => localizer["Validation.FiscalYear.EndDate.Required"].Value)
            .GreaterThan(x => x.StartDate)
                .WithMessage(_ => localizer["Validation.FiscalYear.EndDate.MustBeAfterStart"].Value);
    }
}
