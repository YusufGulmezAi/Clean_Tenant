using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.CompanyUsers;

/// <summary>
/// Belirtilen Site (Company) kapsamındaki kullanıcının tüm rol atamalarını pasif yapar
/// (UserRoleAssignment.IsActive = false).
/// Kullanıcı bu site'ye giriş yapamaz; diğer scope'ları etkilenmez.
/// </summary>
[RequirePermission("Company.Users.Manage")]
public sealed record DeactivateCompanyUserCommand(
    string UserUrlCode,
    Guid TenantId,
    Guid CompanyId) : IRequest<Result>;
