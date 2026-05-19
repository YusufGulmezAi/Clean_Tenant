using MediatR;

namespace CleanTenant.Application.Features.Catalog.Permissions;

public sealed record GetPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;
