using FluentValidation;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>Role formu için FluentValidation validator.</summary>
public sealed class RoleFormValidator : AbstractValidator<RoleFormModel>
{
    public RoleFormValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rol adı zorunludur.")
            .MaximumLength(256).WithMessage("Rol adı 256 karakteri aşamaz.");

        RuleFor(x => x.Scope)
            .InclusiveBetween(0, 3).WithMessage("Geçersiz scope seviyesi.");

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage("Açıklama 1024 karakteri aşamaz.");
    }
}
