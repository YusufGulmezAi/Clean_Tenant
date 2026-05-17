namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Login tarafının kullanıcı persona tipi. Login endpoint'inde zorunlu
/// parametre olarak gönderilir; hangi scope'ların erişilebilir olduğunu
/// belirler. Cross-persona oturum atlamasını engelleyen güvenlik sınırı.
/// </para>
/// <para>
/// <b>Management:</b> ManagementApp ve MobilApp Management persona'sı.
/// İzin verilen scope'lar: System / Tenant / Company.
/// </para>
/// <para>
/// <b>Portal:</b> PortalApp ve MobilApp Portal persona'sı.
/// İzin verilen scope'lar: Unit (Malik / Hissedar / Sakin / Kiracı).
/// </para>
/// </summary>
public enum PersonaSide
{
    /// <summary>Yönetim tarafı — System / Tenant / Company scope'ları.</summary>
    Management = 1,

    /// <summary>Portal tarafı — Unit scope'u (bireysel sakin / malik / kiracı).</summary>
    Portal = 2,
}
