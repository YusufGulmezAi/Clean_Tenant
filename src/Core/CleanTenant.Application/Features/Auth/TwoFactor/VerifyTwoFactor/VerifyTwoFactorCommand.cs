namespace CleanTenant.Application.Features.Auth.TwoFactor.VerifyTwoFactor;

/// <summary>
/// Login akışında 2FA challenge'a yanıt olarak istemcinin gönderdiği komut.
/// </summary>
/// <param name="ChallengeToken">Login response'unda alınan geçici token.</param>
/// <param name="Method">
/// <c>"Authenticator"</c> (TOTP), <c>"Email"</c>, <c>"Phone"</c> veya
/// <c>"RecoveryCode"</c>. Recovery code yöntemi her zaman geçerlidir; diğer
/// yöntemler challenge'ın <c>AvailableMethods</c> listesinde olmalıdır.
/// </param>
/// <param name="Code">Kullanıcının girdiği 6 haneli TOTP/E-posta/SMS kodu veya recovery code.</param>
/// <param name="IpAddress">İstemci IP'si (audit + finalize için).</param>
/// <param name="UserAgent">İstemci User-Agent.</param>
public sealed record VerifyTwoFactorCommand(
    Guid ChallengeToken,
    string Method,
    string Code,
    string IpAddress,
    string UserAgent);
