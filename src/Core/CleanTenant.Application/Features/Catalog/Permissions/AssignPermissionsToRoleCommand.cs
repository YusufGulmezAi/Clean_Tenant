using MediatR;

namespace CleanTenant.Application.Features.Catalog.Permissions;

public sealed record AssignPermissionsToRoleCommand(
    Guid RoleId,
    IReadOnlyList<Guid> PermissionIds) : IRequest;
