namespace CleanTenant.Application.Common.Notifications;

/// <summary>
/// Genel amaçlı e-posta gönderim sözleşmesi. v0.1.5.c'de 2FA kodları için
/// devreye girdi; Faz 1'de e-posta doğrulama, şifre sıfırlama gibi senaryolarda
/// da aynı interface kullanılır.
/// </summary>
public interface IEmailSender
{
    /// <summary>Düz metin e-posta gönderir.</summary>
    /// <param name="to">Alıcı e-posta adresi (tek).</param>
    /// <param name="subject">Konu.</param>
    /// <param name="body">Düz metin gövde; HTML şu an desteklenmez (Faz 1).</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
