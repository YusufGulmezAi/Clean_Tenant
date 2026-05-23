using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.RemoveMethod;

/// <summary>
/// E-posta veya telefon 2FA yöntemini kaldırır (ilgili onay bayrağını temizler).
/// Authenticator (TOTP) kaldırma ayrı komuttadır (DisableTotp). Son yöntemi
/// kaldırma System scope'ta engellenir; non-system'de geriye yöntem kalmazsa
/// <c>TwoFactorEnabled=false</c> yapılır.
/// </summary>
/// <param name="Method">"Email" veya "Phone".</param>
public sealed record RemoveTwoFactorMethodCommand(string Method) : IRequest<Result>;
