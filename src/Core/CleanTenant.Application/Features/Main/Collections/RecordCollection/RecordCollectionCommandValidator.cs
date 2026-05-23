using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Collections.RecordCollection;

/// <summary><see cref="RecordCollectionCommand"/> FluentValidation kuralları.</summary>
public sealed class RecordCollectionCommandValidator : AbstractValidator<RecordCollectionCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public RecordCollectionCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.AccountingPeriodId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.CashAccountCodeId).NotEmpty();
        RuleFor(x => x.Reference).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Reference));
        RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
