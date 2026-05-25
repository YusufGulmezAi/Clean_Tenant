using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Infrastructure.Persistence.Seeding;

/// <summary>
/// <para>
/// CleanTenant'ın built-in (sistem tarafından sağlanan) rollerinin statik
/// kataloğu. Bu roller seed sırasında oluşturulur, <c>IsBuiltIn=true</c>
/// işaretlenir; UI'da silinemez / yeniden adlandırılamaz.
/// </para>
/// <para>
/// <b>İzin haritalaması (karar 2026-05-24):</b>
/// <list type="bullet">
///   <item><c>SystemAdmin</c> — tam erişim operatör rolü; <see cref="CatalogSeeder"/>
///   katalogdaki TÜM izinleri otomatik atar ve yeni izinlerle birlikte büyür.</item>
///   <item><c>TenantAdmin</c> / <c>CompanyAdmin</c> — seed ile izin <b>atanmaz</b>
///   (başlangıçta boş); izinlerini SystemAdmin, Rol Yönetimi ekranından düzenler.</item>
/// </list>
/// </para>
/// <para>
/// <b>Toplam 3 rol:</b> System (1) + Tenant (1) + Company (1). Eski operatör
/// (Developer/Support/Accountant…) ve Unit (Malik/Kiracı…) rolleri kaldırıldı;
/// gerekli olduğunda UI'dan özel rol olarak oluşturulur.
/// </para>
/// </summary>
public static class BuiltInRoleCatalog
{
    /// <summary>Built-in rol tanımı.</summary>
    /// <param name="Name">Rol adı (locale-bağımsız stabil tanımlayıcı).</param>
    /// <param name="Description">Türkçe açıklama (UI'da gösterim için).</param>
    /// <param name="Scope">Rolün uygulanabileceği kapsam seviyesi.</param>
    public sealed record Definition(string Name, string Description, ScopeLevel Scope);

    /// <summary>Tüm built-in roller.</summary>
    public static readonly IReadOnlyList<Definition> All =
    [
        // ---------- System (1) — SaaS işletmecisinin tam yetkili operatörü ----------
        new("SystemAdmin", "Sistem yöneticisi; tam erişim + tüm rollerin izin yönetimi", ScopeLevel.System),

        // ---------- Tenant (1) — izinleri SystemAdmin tarafından atanır (başlangıçta boş) ----------
        new("TenantAdmin", "Tenant yöneticisi; izinleri SystemAdmin tarafından atanır", ScopeLevel.Tenant),

        // ---------- Company (1) — izinleri SystemAdmin tarafından atanır (başlangıçta boş) ----------
        new("CompanyAdmin", "Şirket yöneticisi; izinleri SystemAdmin tarafından atanır", ScopeLevel.Company),
    ];
}
