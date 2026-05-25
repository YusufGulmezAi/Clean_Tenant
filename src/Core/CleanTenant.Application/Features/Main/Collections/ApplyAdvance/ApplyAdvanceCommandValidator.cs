using FluentValidation;

namespace CleanTenant.Application.Features.Main.Collections.ApplyAdvance;

/// <summary><see cref="ApplyAdvanceCommand"/> FluentValidation kuralları.</summary>
public sealed class ApplyAdvanceCommandValidator : AbstractValidator<ApplyAdvanceCommand>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ApplyAdvanceCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
    }
}
