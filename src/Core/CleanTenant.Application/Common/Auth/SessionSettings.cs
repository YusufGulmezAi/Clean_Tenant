namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Redis-backed auth session store ayarları. Session TTL = access token TTL +
/// padding. Kullanıcı aktifse (her HTTP istekte) <c>lastActivity</c> güncellenir
/// ve sliding TTL otomatik uzar.
/// </para>
/// </summary>
public sealed class SessionSettings
{
    /// <summary>Konfigürasyon section adı.</summary>
    public const string SectionName = "Session";

    /// <summary>Access token TTL'ine eklenen padding (dakika). Varsayılan 30.</summary>
    public int TtlPaddingMinutes { get; set; } = 30;

    /// <summary>
    /// Redis anahtar prefix'i. Çoklu uygulama Redis'i paylaşıyorsa ayrışma için.
    /// Varsayılan: "ct" (CleanTenant).
    /// </summary>
    public string KeyPrefix { get; set; } = "ct";
}
