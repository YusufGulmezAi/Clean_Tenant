namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// JWT üretimi ve doğrulaması için konfigürasyon. <c>IOptions&lt;JwtSettings&gt;</c>
/// ile DI'a kaydedilir. Konfigürasyon kaynakları:
/// <list type="bullet">
///   <item><c>JWT_ISSUER</c>, <c>JWT_AUDIENCE</c></item>
///   <item><c>JWT_SIGNING_KEY</c> — HMAC SHA-256 için min 32 byte UTF-8</item>
///   <item><c>JWT_ACCESS_TOKEN_MINUTES</c> — varsayılan 15 (System), 30 (diğer)</item>
///   <item><c>JWT_REFRESH_TOKEN_DAYS</c> — varsayılan 7</item>
/// </list>
/// </para>
/// </summary>
public sealed class JwtSettings
{
    /// <summary>Konfigürasyon section adı (IConfiguration.GetSection için).</summary>
    public const string SectionName = "Jwt";

    /// <summary>Token issuer.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Token audience.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC SHA-256 imza anahtarı (UTF-8 string, min 32 byte).</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token süresi (dakika).</summary>
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>Refresh token süresi (gün).</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
