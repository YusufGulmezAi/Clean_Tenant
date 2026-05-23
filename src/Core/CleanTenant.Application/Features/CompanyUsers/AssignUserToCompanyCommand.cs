using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.System.Users;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.CompanyUsers;

/// <summary>
/// Sistemde zaten var olan bir kullanıcıyı belirtilen Site'ye (Company) Company-scope rollerle atar.
/// Yeni kullanıcı oluşturmaz; şifre gerekmez.
/// </summary>
[RequirePermission("Company.Users.Manage")]
public sealed record AssignUserToCompanyCommand(
    Guid TenantId,
    Guid CompanyId,
    string Email,
    IReadOnlyList<Guid> RoleIds) : IRequest<Result<UserListItem>>;
