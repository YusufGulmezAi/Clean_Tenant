using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Tek bir kullanıcının detayını (ve scope'taki atamalarını) döner.
/// </summary>
[RequirePermission("System.Users.Manage", "User.Read")]
public sealed record GetUserDetailQuery(
    string UrlCode,
    ScopeLevel Scope,
    Guid? TenantId = null,
    Guid? CompanyId = null) : IRequest<Result<UserDetail>>;
