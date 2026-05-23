using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accruals.DistributeInvoice;

/// <summary><see cref="DistributeInvoiceAmongUnitsCommand"/> FluentValidation kuralları.</summary>
public sealed class DistributeInvoiceAmongUnitsCommandValidator
    : AbstractValidator<DistributeInvoiceAmongUnitsCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public DistributeInvoiceAmongUnitsCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.AccountingPeriodId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.TotalAmount).GreaterThan(0m);
        RuleFor(x => x.ReceivableAccountCodeId).NotEmpty();
        RuleFor(x => x.IncomeAccountCodeId).NotEmpty();
        RuleFor(x => x.DueDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}
