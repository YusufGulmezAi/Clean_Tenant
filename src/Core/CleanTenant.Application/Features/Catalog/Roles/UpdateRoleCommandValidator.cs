using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.Roles;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role adı zorunludur.")
            .MaximumLength(256).WithMessage("Role adı 256 karakteri aşamaz.");

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage("Açıklama 1024 karakteri aşamaz.");
    }
}
