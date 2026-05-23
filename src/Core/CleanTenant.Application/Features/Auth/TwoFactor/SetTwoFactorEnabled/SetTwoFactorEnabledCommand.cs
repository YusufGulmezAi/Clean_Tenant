using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.SetTwoFactorEnabled;

/// <summary>
/// 2FA ana açma/kapama anahtarı. Açmak için en az bir doğrulanmış yöntem
/// (Authenticator / Email / Phone) gerekir. System scope kullanıcılar 2FA'yı
/// kapatamaz (zorunlu). System dışı kullanıcılar 2FA'yı <b>pasife alırken</b>
/// güvenlik gereği hesap şifrelerini doğrulamalıdır.
/// </summary>
/// <param name="Enabled">Yeni durum: <c>true</c> = aktif, <c>false</c> = pasif.</param>
/// <param name="Password">
/// Yalnız pasife alma (System dışı) için: hesap şifresi. Açma/idempotent
/// durumlarda yok sayılır.
/// </param>
public sealed record SetTwoFactorEnabledCommand(bool Enabled, string? Password = null) : IRequest<Result>;
