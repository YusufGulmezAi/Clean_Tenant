using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary><see cref="RemoveBudgetLineVersionCommand"/> FluentValidation kuralları.</summary>
public sealed class RemoveBudgetLineVersionCommandValidator : AbstractValidator<RemoveBudgetLineVersionCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveBudgetLineVersionCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BudgetLineVersionId).NotEmpty();
    }
}
