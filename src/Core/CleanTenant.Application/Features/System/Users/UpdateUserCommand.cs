using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Kullanıcı bilgilerini ve scope'taki rol atamalarını günceller.
/// Tüm scope'larda (System / Tenant / Company) kullanılır.
/// </summary>
[RequirePermission("System.Users.Manage", "User.Update")]
public sealed record UpdateUserCommand(
    string UrlCode,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ScopeLevel Scope,
    Guid? TenantId,
    Guid? CompanyId,
    IReadOnlyList<Guid> RoleIds) : IRequest<Result<UserListItem>>;
