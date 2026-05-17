namespace CleanTenant.SharedKernel.Context;

/// <summary>
/// <para>
/// Mevcut isteğin (request) tenant bağlamını ifade eder. Aktif tenant /
/// company / unit kimliklerini ve hangi <see cref="ScopeLevel"/>'da
/// çalışıldığını taşır.
/// </para>
/// <para>
/// <b>Implementasyon konumu:</b> Concrete sınıf <c>Infrastructure.Identity</c>
/// içinde yaşar; bilgi JWT claim'lerinden okunur (her tab kendi context
/// token'ına sahiptir).
/// </para>
/// <para>
/// <b>Kullanım yerleri:</b> Multi-tenancy global query filter, audit
/// interceptor (TenantId otomatik doldurma), authorization behavior
/// (request hedef tenant'la JWT claim'ini eşleştirme).
/// </para>
/// </summary>
public interface ITenantContext
{
    /// <summary>Aktif tenant; System scope'unda veya henüz seçilmemişse null.</summary>
    Guid? TenantId { get; }

    /// <summary>Aktif şirket; Tenant veya üstü scope'lardaysa null.</summary>
    Guid? CompanyId { get; }

    /// <summary>Aktif bağımsız bölüm (Unit); Company veya üstü scope'lardaysa null.</summary>
    Guid? UnitId { get; }

    /// <summary>İçinde bulunulan yetki kapsamı seviyesi.</summary>
    ScopeLevel CurrentScope { get; }
}
