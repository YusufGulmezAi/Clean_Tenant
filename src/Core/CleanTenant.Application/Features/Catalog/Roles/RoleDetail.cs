using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Features.Catalog.Roles;

/// <summary>
/// Rol detay projection'ı. <see cref="TenantId"/> ve <see cref="CompanyId"/>
/// null ise global rol, dolu ise tenant/company sahipli custom rol (v0.2.8.b).
/// <see cref="UrlCode"/> 9 karakterlik Base58 — UI rotalarında GUID yerine
/// kullanılır (v0.2.9.a).
/// </summary>
public sealed record RoleDetail(
    Guid Id,
    string UrlCode,
    string Name,
    ScopeLevel Scope,
    string? Description,
    bool IsBuiltIn,
    Guid? TenantId,
    Guid? CompanyId,
    IReadOnlyList<Guid> PermissionIds);
