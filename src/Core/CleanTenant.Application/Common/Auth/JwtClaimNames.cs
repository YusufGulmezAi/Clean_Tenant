namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// CleanTenant JWT'lerinde kullanılan claim adlarının stabil sabitleri.
/// JWT thin tutulduğu için sadece referans claim'leri burada; zengin
/// claim'ler (roller, permission'lar, scope detayı) Redis session'da.
/// </para>
/// </summary>
public static class JwtClaimNames
{
    /// <summary>Standart "sub" — kullanıcının Id'si.</summary>
    public const string Subject = "sub";

    /// <summary>Redis session lookup key.</summary>
    public const string SessionId = "sid";

    /// <summary>Sekme / persona izolasyonu için context kimliği.</summary>
    public const string ContextId = "ctx";

    /// <summary>JWT issued-at zamanı (Unix epoch).</summary>
    public const string IssuedAt = "iat";

    /// <summary>JWT expiry zamanı (Unix epoch).</summary>
    public const string Expiry = "exp";

    /// <summary>JWT issuer.</summary>
    public const string Issuer = "iss";

    /// <summary>JWT audience.</summary>
    public const string Audience = "aud";
}
