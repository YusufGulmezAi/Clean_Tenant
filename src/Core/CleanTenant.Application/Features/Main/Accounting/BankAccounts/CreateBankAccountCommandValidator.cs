using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Accounting.BankAccounts;

/// <summary>
/// <see cref="CreateBankAccountCommand"/> FluentValidation kuralları.
/// </summary>
public sealed class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBankAccountCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CompanyId"].Value);

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "TenantId"].Value);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "Name"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.MaxLength", "Name", 256].Value);

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "BankName"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.MaxLength", "BankName", 256].Value);

        RuleFor(x => x.BranchCode)
            .MaximumLength(20)
                .When(x => !string.IsNullOrWhiteSpace(x.BranchCode), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "AccountNumber"].Value)
            .MaximumLength(64).WithMessage(_ => localizer["Validation.MaxLength", "AccountNumber", 64].Value);

        // IBAN: TR + 24 rakam (toplam 26 karakter)
        RuleFor(x => x.Iban)
            .Matches(@"^TR[0-9]{24}$")
                .When(x => !string.IsNullOrEmpty(x.Iban))
                .WithMessage("Geçersiz IBAN formatı. TR ile başlayan 26 karakter olmalıdır.")
                .WithErrorCode("ACC-401");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "CurrencyCode"].Value)
            .Length(3).WithMessage(_ => localizer["Validation.ExactLength", "CurrencyCode", 3].Value);
    }
}
