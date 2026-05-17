using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.DisableTotp;

/// <summary>
/// Authenticated kullanıcının kendi TOTP yöntemini kapat isteği.
/// System scope rolü olan kullanıcıda son aktif yöntem TOTP ise reddedilir
/// (<c>AUTH-2FA-LAST-METHOD-LOCK</c>).
/// </summary>
public sealed record DisableTotpCommand : IRequest<Result>;
