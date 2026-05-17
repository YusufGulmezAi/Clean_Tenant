namespace CleanTenant.Application.Common.Notifications;

/// <summary>
/// Genel amaçlı SMS gönderim sözleşmesi. v0.1.5.c'de 2FA kodları için
/// devreye girdi; Faz 1'de telefon doğrulama, OTP işlemleri için de kullanılır.
/// </summary>
public interface ISmsSender
{
    /// <summary>SMS mesajı gönderir.</summary>
    /// <param name="toPhone">Alıcı telefon numarası (E.164 formatı: +905xxxxxxxxx).</param>
    /// <param name="message">Gönderilecek mesaj (genellikle 160 karakter altı).</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    Task SendAsync(string toPhone, string message, CancellationToken cancellationToken = default);
}
