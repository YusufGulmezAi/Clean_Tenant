using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary><see cref="SaveBudgetAsTemplateCommand"/> FluentValidation kuralları.</summary>
public sealed class SaveBudgetAsTemplateCommandValidator : AbstractValidator<SaveBudgetAsTemplateCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public SaveBudgetAsTemplateCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.SourceBudgetId).NotEmpty();
        RuleFor(x => x.TemplateName)
            .NotEmpty().WithMessage(_ => localizer["Validation.Required", "TemplateName"].Value)
            .MaximumLength(200).WithMessage(_ => localizer["Validation.MaxLength", "TemplateName", 200].Value);
        RuleFor(x => x.Description)
            .MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Description));
        RuleFor(x => x.Visibility).IsInEnum();
    }
}
