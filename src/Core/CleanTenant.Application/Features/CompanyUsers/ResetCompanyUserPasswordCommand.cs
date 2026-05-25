using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.CompanyUsers;

/// <summary>
/// Belirtilen Site (Company) kapsamındaki kullanıcının şifresini sıfırlar (admin reset;
/// mevcut şifre doğrulaması / token akışı yoktur). Hedef kullanıcının bu site'de aktif bir
/// Company-scope ataması olmalıdır — aksi halde işlem reddedilir (sahiplik guard'ı).
/// </summary>
[RequirePermission("Company.Users.Manage")]
public sealed record ResetCompanyUserPasswordCommand(
    string UserUrlCode,
    string NewPassword,
    Guid TenantId,
    Guid CompanyId) : IRequest<Result>;
