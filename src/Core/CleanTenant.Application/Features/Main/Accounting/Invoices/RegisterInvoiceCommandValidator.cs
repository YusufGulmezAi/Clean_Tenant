using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.Invoices;

/// <summary>
/// <see cref="RegisterInvoiceCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class RegisterInvoiceCommandValidator : AbstractValidator<RegisterInvoiceCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public RegisterInvoiceCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "TenantId"].Value);

        RuleFor(x => x.AccountingPeriodId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "AccountingPeriodId"].Value);

        RuleFor(x => x.InvoiceNumber)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "InvoiceNumber"].Value)
            .MaximumLength(64).WithMessage(_ => localizer["Validation.MaxLength", "InvoiceNumber", 64].Value);

        RuleFor(x => x.InvoiceDate)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "InvoiceDate"].Value);

        RuleFor(x => x.CounterpartyName)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CounterpartyName"].Value)
            .MaximumLength(512).WithMessage(_ => localizer["Validation.MaxLength", "CounterpartyName", 512].Value);

        RuleFor(x => x.CounterpartyTaxId)
            .MaximumLength(32)
                .When(x => !string.IsNullOrWhiteSpace(x.CounterpartyTaxId), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.AccountCodeId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "AccountCodeId"].Value);

        RuleFor(x => x.SubTotal)
            .GreaterThan(0).WithMessage(_ => localizer["Validation.MustBePositive", "SubTotal"].Value);

        RuleFor(x => x.VatAmount)
            .GreaterThanOrEqualTo(0).WithMessage(_ => localizer["Validation.MustBeNonNegative", "VatAmount"].Value);

        RuleFor(x => x.Notes)
            .MaximumLength(2048)
                .When(x => !string.IsNullOrWhiteSpace(x.Notes), ApplyConditionTo.CurrentValidator);
    }
}
