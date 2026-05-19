namespace CleanTenant.Application.Features.Catalog.Permissions;

public sealed record PermissionDto(
    Guid Id,
    string Code,
    string Description,
    string Module);
