using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Tenant.Users;

/// <summary>
/// Belirtilen tenant kapsamındaki kullanıcının tüm rol atamalarını pasif yapar
/// (UserRoleAssignment.IsActive = false).
/// Kullanıcı bu tenant'a giriş yapamaz; diğer scope'ları etkilenmez.
/// </summary>
[RequirePermission("Tenant.Users.Manage")]
public sealed record DeactivateTenantUserCommand(
    string UserUrlCode,
    Guid TenantId) : IRequest<Result>;
