using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.System.Users;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.CompanyUsers;

/// <summary>
/// Belirtilen Site (Company) için Company-scope kullanıcı oluşturur ve en az bir Company rolü atar.
/// </summary>
[RequirePermission("Company.Users.Manage")]
public sealed record CreateCompanyUserCommand(
    Guid TenantId,
    Guid CompanyId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    IReadOnlyList<Guid> RoleIds) : IRequest<Result<UserListItem>>;
