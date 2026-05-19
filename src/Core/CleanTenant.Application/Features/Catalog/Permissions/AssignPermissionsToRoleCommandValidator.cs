using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.Permissions;

public sealed class AssignPermissionsToRoleCommandValidator : AbstractValidator<AssignPermissionsToRoleCommand>
{
    public AssignPermissionsToRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID zorunludur.");

        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("Permission ID listesi zorunludur.");
    }
}
