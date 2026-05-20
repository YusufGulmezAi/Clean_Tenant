using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>Role formu için FluentValidation validator. v0.2.11.d — lokalize.</summary>
public sealed class RoleFormValidator : AbstractValidator<RoleFormModel>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public RoleFormValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.Role.Name.Required"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.Role.Name.MaxLength", 256].Value);

        RuleFor(x => x.Scope)
            .InclusiveBetween(0, 3).WithMessage(_ => localizer["Validation.Role.Scope.Invalid"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage(_ => localizer["Validation.Role.Description.MaxLength", 1024].Value);
    }
}
