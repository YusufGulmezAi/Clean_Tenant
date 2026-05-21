using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.System.Users;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Tenant.Users;

/// <summary>
/// Sistemde zaten var olan bir kullanıcıyı belirtilen Tenant'a Tenant-scope rollerle atar.
/// Yeni kullanıcı oluşturmaz; şifre gerekmez.
/// </summary>
[RequirePermission("Tenant.Users.Manage")]
public sealed record AssignUserToTenantCommand(
    Guid TenantId,
    string Email,
    IReadOnlyList<Guid> RoleIds) : IRequest<Result<UserListItem>>;
