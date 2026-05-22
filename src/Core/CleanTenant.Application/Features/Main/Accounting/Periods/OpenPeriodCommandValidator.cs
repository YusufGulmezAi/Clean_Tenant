using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>
/// <see cref="OpenPeriodCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class OpenPeriodCommandValidator : AbstractValidator<OpenPeriodCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public OpenPeriodCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.PeriodId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "PeriodId"].Value);
    }
}
