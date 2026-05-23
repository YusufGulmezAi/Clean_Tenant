using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.LateFees.GenerateLateFeeCharges;

/// <summary><see cref="GenerateLateFeeChargesCommand"/> FluentValidation kuralları.</summary>
public sealed class GenerateLateFeeChargesCommandValidator
    : AbstractValidator<GenerateLateFeeChargesCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public GenerateLateFeeChargesCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.AsOfDate).NotEmpty();
    }
}
