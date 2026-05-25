using FluentValidation;

namespace CleanTenant.Application.Features.Main.Collections.RefundAdvance;

/// <summary><see cref="RefundAdvanceCommand"/> FluentValidation kuralları.</summary>
public sealed class RefundAdvanceCommandValidator : AbstractValidator<RefundAdvanceCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public RefundAdvanceCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.RefundDate).NotEmpty();
        RuleFor(x => x.CashAccountCodeId).NotEmpty();
        RuleFor(x => x.Reference).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Reference));
        RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
