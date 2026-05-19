using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Features.Catalog.Roles;

public sealed record RoleListItem(
    Guid Id,
    string Name,
    ScopeLevel Scope,
    string? Description,
    bool IsBuiltIn,
    int PermissionCount);
