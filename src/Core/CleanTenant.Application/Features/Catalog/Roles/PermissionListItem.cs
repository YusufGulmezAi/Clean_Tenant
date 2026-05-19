using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Features.Catalog.Roles;

/// <summary>
/// Rol Yönetimi izin paneli için projection DTO'su. <see cref="MinimumRoleScope"/>
/// alanı privilege ceiling filtresinde kullanılır (bkz. v0.2.8.a).
/// </summary>
public sealed record PermissionListItem(
    Guid Id,
    string Code,
    string Description,
    string Module,
    ScopeLevel MinimumRoleScope);
