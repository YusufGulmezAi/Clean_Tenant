using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Verilen scope için atanabilir rol seçeneklerini döner.
/// System scope → yalnız System rolleri.
/// Tenant scope → global + tenant'a ait Tenant rolleri.
/// Company scope → global + tenant + company'ye ait Company rolleri.
/// </summary>
[RequirePermission("System.Users.Manage", "User.Create", "User.Update")]
public sealed record GetRoleOptionsQuery(
    ScopeLevel Scope,
    Guid? TenantId = null,
    Guid? CompanyId = null) : IRequest<Result<IReadOnlyList<RoleOption>>>;
