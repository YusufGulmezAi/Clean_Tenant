using CleanTenant.Domain.Tenant.LateFees;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.LateFees.SetLateFeePolicy;

/// <summary><see cref="SetLateFeePolicyCommand"/> FluentValidation kuralları.</summary>
public sealed class SetLateFeePolicyCommandValidator : AbstractValidator<SetLateFeePolicyCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public SetLateFeePolicyCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.IncomeAccountCodeId).NotEmpty();

        // KMK m.20: 0 < oran ≤ aylık %5 tavan
        RuleFor(x => x.MonthlyRatePercent)
            .GreaterThan(0m)
            .LessThanOrEqualTo(RegulatoryLimits.KmkM20MonthlyCapPercent);

        RuleFor(x => x.GraceDays).GreaterThanOrEqualTo(0);
    }
}
