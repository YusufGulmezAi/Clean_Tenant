using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Tenant.Users;

/// <summary>
/// Belirtilen tenant kapsamındaki kullanıcının pasif rol atamalarını tekrar aktif yapar
/// (UserRoleAssignment.IsActive = true).
/// </summary>
[RequirePermission("Tenant.Users.Manage")]
public sealed record ReactivateTenantUserCommand(
    string UserUrlCode,
    Guid TenantId) : IRequest<Result>;
