namespace CleanTenant.Application.Features.Auth.TwoFactor.SendCode;

/// <summary>
/// Login challenge sırasında E-posta veya SMS yöntemiyle doğrulama kodu gönderim isteği.
/// TOTP'de kullanılmaz (kullanıcı kendi authenticator app'inden okur).
/// </summary>
/// <param name="ChallengeToken">Login response'unda alınan geçici token.</param>
/// <param name="Method"><c>"Email"</c> veya <c>"Phone"</c>.</param>
public sealed record SendTwoFactorCodeCommand(Guid ChallengeToken, string Method);
