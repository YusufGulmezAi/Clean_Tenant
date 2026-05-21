namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// Kullanıcı kimlik doğrulama amaçlı kısa süreli OTP kodlarını yöneten servis.
/// Tipik kullanım: kullanıcı onboarding sırasında TCKN/VKN/YKN, telefon veya
/// e-posta doğrulaması. Redis ile TTL-bazlı otomatik silme.
/// </summary>
public interface IVerificationCodeService
{
    /// <summary>
    /// Belirtilen anahtar için 6 haneli sayısal OTP kodu üretir ve saklar.
    /// Aynı anahtara yeni kod üretilirse eskileri geçersiz olur (overwrite).
    /// </summary>
    /// <param name="key">Benzersiz doğrulama anahtarı (örn. "phone:+905xx", "email:user@x.com").</param>
    /// <param name="ttl">Kodun geçerlilik süresi.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>Üretilen 6 haneli kod (SMS/e-posta ile gönderilecek).</returns>
    Task<string> GenerateAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kodu doğrular. Eşleşirse kodu siler (tek kullanımlık) ve true döner.
    /// Eşleşmezse veya süre dolmuşsa false döner.
    /// </summary>
    /// <param name="key">Doğrulama anahtarı.</param>
    /// <param name="code">Kullanıcının girdiği kod.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    Task<bool> VerifyAsync(string key, string code, CancellationToken cancellationToken = default);

    /// <summary>Kodu silir (akış iptal edildiğinde cleanup).</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
