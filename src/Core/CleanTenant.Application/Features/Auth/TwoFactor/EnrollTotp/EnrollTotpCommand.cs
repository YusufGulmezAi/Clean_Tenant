namespace CleanTenant.Application.Features.Auth.TwoFactor.EnrollTotp;

/// <summary>
/// Authenticated kullanıcının kendi hesabı için TOTP enrollment başlat isteği.
/// Komut gövdesi alanı yok — kim olduğu Bearer JWT'den okunur.
/// </summary>
public sealed record EnrollTotpCommand();

/// <summary>
/// Enrollment başlangıç yanıtı. <see cref="Secret"/> kullanıcıya gösterilir ya
/// da <see cref="QrCodeUri"/> bir QR kod kütüphanesiyle resmedilir (otpauth URI).
/// </summary>
/// <param name="Secret">Base32 secret (manuel giriş için).</param>
/// <param name="QrCodeUri">otpauth://totp/... URI; QR kod üretiminde kullanılır.</param>
public sealed record EnrollTotpResult(string Secret, string QrCodeUri);
