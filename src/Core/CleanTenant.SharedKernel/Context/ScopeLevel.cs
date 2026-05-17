namespace CleanTenant.SharedKernel.Context;

/// <summary>
/// <para>
/// Bir kullanıcının aktif olarak iş yaptığı yetki kapsamının hiyerarşi
/// seviyesi. ManagementApp ve PortalApp'te bağlam (context) değişimi
/// gerçekleştikçe bu seviye değişir; JWT token'ı buna göre yeniden üretilir.
/// </para>
/// <para>
/// Hiyerarşi: <c>System &gt; Tenant &gt; Company &gt; Unit</c>. Building
/// yönetim hiyerarşisinde bilinçli olarak <b>scope seviyesi değildir</b>;
/// rol atamaları doğrudan Unit'e yapılır.
/// </para>
/// </summary>
public enum ScopeLevel
{
    /// <summary>Bağlam yok; kimliği doğrulanmamış ya da bağlam seçimi yapılmamış.</summary>
    None = 0,

    /// <summary>Sistem geneli (tüm tenant'lar üstü) yetki kapsamı.</summary>
    System = 1,

    /// <summary>Belirli bir tenant kapsamı.</summary>
    Tenant = 2,

    /// <summary>Bir tenant içindeki belirli bir şirket kapsamı.</summary>
    Company = 3,

    /// <summary>Bir şirket altındaki belirli bir bağımsız bölüm (Unit) kapsamı (Malik/Sakin/Kiracı vb.).</summary>
    Unit = 4,
}
