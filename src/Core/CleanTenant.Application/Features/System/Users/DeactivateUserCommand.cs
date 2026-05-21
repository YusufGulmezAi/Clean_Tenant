using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Kullanıcıyı soft-delete ile devre dışı bırakır.
/// Tüm aktif session'ları sonlandırılır (ForceLogout entegre değil — UI ayrıca çağırır).
/// </summary>
[RequirePermission("System.Users.Manage", "User.Delete")]
public sealed record DeactivateUserCommand(string UrlCode) : IRequest<Result>;
