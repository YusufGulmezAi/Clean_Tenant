using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Belirtilen scope'taki kullanıcıları listeler.
/// System scope için TenantId/CompanyId null olmalıdır.
/// </summary>
[RequirePermission("System.Users.Manage", "User.Read")]
public sealed record ListUsersQuery(
    ScopeLevel Scope,
    Guid? TenantId = null,
    Guid? CompanyId = null,
    string? Search = null) : IRequest<Result<IReadOnlyList<UserListItem>>>;
