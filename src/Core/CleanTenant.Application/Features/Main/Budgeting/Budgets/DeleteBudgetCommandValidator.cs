using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary><see cref="DeleteBudgetCommand"/> FluentValidation kuralları.</summary>
public sealed class DeleteBudgetCommandValidator : AbstractValidator<DeleteBudgetCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteBudgetCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BudgetId).NotEmpty();
    }
}
