using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.Roles;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role adı zorunludur.")
            .MaximumLength(256).WithMessage("Role adı 256 karakteri aşamaz.");

        RuleFor(x => x.Scope)
            .InclusiveBetween(0, 3).WithMessage("Geçersiz scope seviyesi.");

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage("Açıklama 1024 karakteri aşamaz.");
    }
}
