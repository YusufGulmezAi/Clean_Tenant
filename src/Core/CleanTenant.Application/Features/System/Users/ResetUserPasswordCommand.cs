using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Admin tarafından kullanıcı şifresi sıfırlama. Mevcut şifre doğrulaması gerekmez.
/// </summary>
[RequirePermission("System.Users.Manage", "User.ResetPassword")]
public sealed record ResetUserPasswordCommand(
    string UrlCode,
    string NewPassword) : IRequest<Result>;
