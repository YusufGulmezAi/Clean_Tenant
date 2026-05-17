namespace CleanTenant.Application.Features.Auth.TwoFactor.ConfirmTotpEnrollment;

/// <summary>
/// TOTP enrollment'ın ikinci adımı: kullanıcı authenticator app'ten ilk kodu girer,
/// doğruysa <c>TwoFactorEnabled=true</c> ve 10 recovery code üretilir.
/// </summary>
/// <param name="Code">Authenticator app'in ürettiği 6 haneli kod.</param>
public sealed record ConfirmTotpEnrollmentCommand(string Code);

/// <summary>
/// Onay yanıtı: 10 recovery code (her biri tek kullanımlık). Bu liste yalnız
/// bir kere döner — kullanıcı kaydetmeli.
/// </summary>
/// <param name="RecoveryCodes">10 adet tek kullanımlık kurtarma kodu.</param>
public sealed record ConfirmTotpEnrollmentResult(IReadOnlyList<string> RecoveryCodes);
