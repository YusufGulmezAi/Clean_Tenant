using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.CompanyUsers;

/// <summary>
/// Belirtilen Site (Company) kapsamındaki kullanıcının pasif rol atamalarını tekrar aktif yapar
/// (UserRoleAssignment.IsActive = true).
/// </summary>
[RequirePermission("Company.Users.Manage")]
public sealed record ReactivateCompanyUserCommand(
    string UserUrlCode,
    Guid TenantId,
    Guid CompanyId) : IRequest<Result>;
