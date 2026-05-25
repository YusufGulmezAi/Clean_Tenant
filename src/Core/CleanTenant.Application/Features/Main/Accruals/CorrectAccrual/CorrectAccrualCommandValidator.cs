using FluentValidation;

namespace CleanTenant.Application.Features.Main.Accruals.CorrectAccrual;

/// <summary><see cref="CorrectAccrualCommand"/> FluentValidation kuralları.</summary>
public sealed class CorrectAccrualCommandValidator : AbstractValidator<CorrectAccrualCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public CorrectAccrualCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.AccrualDetailId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Reason).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
