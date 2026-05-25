using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Tenant.Users;

/// <summary>
/// Belirtilen tenant kapsamındaki kullanıcının şifresini sıfırlar (admin reset; mevcut
/// şifre doğrulaması / token akışı yoktur). Hedef kullanıcının bu tenant'ta aktif bir
/// Tenant-scope ataması olmalıdır — aksi halde işlem reddedilir (sahiplik guard'ı).
/// </summary>
[RequirePermission("Tenant.Users.Manage")]
public sealed record ResetTenantUserPasswordCommand(
    string UserUrlCode,
    string NewPassword,
    Guid TenantId) : IRequest<Result>;
