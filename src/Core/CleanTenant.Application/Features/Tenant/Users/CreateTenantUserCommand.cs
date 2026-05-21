using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.System.Users;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Tenant.Users;

/// <summary>
/// Belirtilen Tenant'a Tenant-scope kullanıcı oluşturur ve en az bir Tenant rolü atar.
/// </summary>
[RequirePermission("Tenant.Users.Manage")]
public sealed record CreateTenantUserCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    IReadOnlyList<Guid> RoleIds) : IRequest<Result<UserListItem>>;
