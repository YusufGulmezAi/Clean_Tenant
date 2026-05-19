namespace CleanTenant.Application.Features.Catalog.Roles;

public sealed record PermissionListItem(
    Guid Id,
    string Code,
    string Description,
    string Module);
