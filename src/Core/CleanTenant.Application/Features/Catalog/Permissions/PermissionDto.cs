using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Features.Catalog.Permissions;

/// <summary>
/// Permission projection DTO'su. <see cref="MinimumRoleScope"/> alanı
/// privilege ceiling kontrolü için UI'nın izinleri scope'a göre filtrelemesini
/// sağlar (bkz. v0.2.8.a).
/// </summary>
public sealed record PermissionDto(
    Guid Id,
    string Code,
    string Description,
    string Module,
    ScopeLevel MinimumRoleScope);
