using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Features.Catalog.Roles;

public sealed record RoleDetail(
    Guid Id,
    string Name,
    ScopeLevel Scope,
    string? Description,
    bool IsBuiltIn,
    IReadOnlyList<Guid> PermissionIds);
