namespace CleanTenant.Application.Common.Auth;

/// <summary>2FA ile ilgili sabitlerin tek doğruluk kaynağı.</summary>
public static class TwoFactorDefaults
{
    /// <summary>
    /// Enrollment ve yeniden üretimde oluşturulan kurtarma kodu sayısı. v0.2.13'te
    /// 10'dan 12'ye çıkarıldı.
    /// </summary>
    public const int RecoveryCodeCount = 12;

    /// <summary>Authenticator (TOTP) yöntem adı.</summary>
    public const string AuthenticatorMethod = "Authenticator";

    /// <summary>E-posta doğrulama yöntem adı.</summary>
    public const string EmailMethod = "Email";

    /// <summary>Telefon (SMS) doğrulama yöntem adı.</summary>
    public const string PhoneMethod = "Phone";
}
