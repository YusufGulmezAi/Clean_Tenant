using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accruals.DirectCharge;

/// <summary><see cref="CreateDirectUnitChargeCommand"/> FluentValidation kuralları.</summary>
public sealed class CreateDirectUnitChargeCommandValidator
    : AbstractValidator<CreateDirectUnitChargeCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateDirectUnitChargeCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.AccountingPeriodId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.ReceivableAccountCodeId).NotEmpty();
        RuleFor(x => x.IncomeAccountCodeId).NotEmpty();
        RuleFor(x => x.DueDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}
