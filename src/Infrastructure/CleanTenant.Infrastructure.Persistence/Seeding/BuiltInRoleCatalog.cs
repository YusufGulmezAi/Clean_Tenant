using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Infrastructure.Persistence.Seeding;

/// <summary>
/// <para>
/// CleanTenant'ın built-in (sistem tarafından sağlanan) rollerinin statik
/// kataloğu. Bu roller seed sırasında oluşturulur, <c>IsBuiltIn=true</c>
/// işaretlenir; UI'da silinemez / yeniden adlandırılamaz.
/// </para>
/// <para>
/// <b>Permission haritalaması Faz 1'e ertelendi.</b> Şimdi sadece rol kayıtları
/// oluşturulur; permission atamaları yapılmaz.
/// </para>
/// <para>
/// <b>Toplam 13 rol:</b> System (7) + Tenant (1) + Company (1) + Unit (4).
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
        // ---------- System (7) — SaaS işletmecisinin personeli ----------
        new("Developer", "Geliştirici; debug ve teknik müdahale için tam erişim", ScopeLevel.System),
        new("SystemAdmin", "Sistem yöneticisi; tenant onboarding, ortam ayarları", ScopeLevel.System),
        new("CustomerSupport", "Müşteri destek temsilcisi; Support Mode'da tenant'a erişir", ScopeLevel.System),
        new("TechnicalSupport", "Teknik destek; log/audit görüntüleme, derin erişim", ScopeLevel.System),
        new("Accountant", "Muhasebe; fatura ve ödeme yönetimi", ScopeLevel.System),
        new("Manager", "Yönetici; iş metrikleri, raporlar", ScopeLevel.System),
        new("Sales", "Satış; demo erişimi, lead yönetimi", ScopeLevel.System),

        // ---------- Tenant (1 minimum) ----------
        new("TenantAdmin", "Tenant yöneticisi; tenant geneli tam yetki", ScopeLevel.Tenant),

        // ---------- Company (1 minimum) ----------
        new("CompanyAdmin", "Şirket yöneticisi; şirket geneli tam yetki", ScopeLevel.Company),

        // ---------- Unit (4) — Bağımsız bölüm sakini rolleri ----------
        new("Malik", "Bağımsız bölümün maliki (tam mülkiyet)", ScopeLevel.Unit),
        new("Hissedar", "Bağımsız bölümün hissedar maliki (paylı mülkiyet)", ScopeLevel.Unit),
        new("Sakin", "Bağımsız bölümde oturan kişi (mülk sahibi olmayan)", ScopeLevel.Unit),
        new("Kiracı", "Bağımsız bölümün kiracısı", ScopeLevel.Unit),
    ];
}
