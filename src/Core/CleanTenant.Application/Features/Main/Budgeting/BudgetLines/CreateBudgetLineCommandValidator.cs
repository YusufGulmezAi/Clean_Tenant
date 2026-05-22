using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLines;

/// <summary><see cref="CreateBudgetLineCommand"/> FluentValidation kuralları.</summary>
public sealed class CreateBudgetLineCommandValidator : AbstractValidator<CreateBudgetLineCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBudgetLineCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.ExpenseCategoryId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Description));
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
