namespace CleanTenant.Infrastructure.Persistence.Seeding;

/// <summary>
/// <para>
/// CleanTenant'ta tanımlı tüm permission kodlarının statik kataloğu.
/// Format: <c>"&lt;Module&gt;.&lt;Action&gt;[.&lt;Qualifier&gt;]"</c>.
/// Yeni permission ekleyince listeyi güncellersin; <see cref="CatalogSeeder"/>
/// idempotent biçimde DB'ye işler.
/// </para>
/// <para>
/// <b>Detaylı permission-rol haritalaması Faz 1'de ManagementApp Rol
/// Yönetimi ekranıyla yapılır;</b> bu sınıf yalnız kod listesi taşır.
/// </para>
/// </summary>
public static class PermissionCatalog
{
    /// <summary>Permission tanımı (kod + açıklama + modül).</summary>
    /// <param name="Code">Stabil permission kodu.</param>
    /// <param name="Description">İnsan okunur açıklama.</param>
    /// <param name="Module">Gruplama amaçlı modül adı.</param>
    public sealed record Definition(string Code, string Description, string Module);

    /// <summary>Permission kataloğu — yeni eklemeler için liste sonuna ekle.</summary>
    public static readonly IReadOnlyList<Definition> All =
    [
        // ---------- Identity & Authorization ----------
        new("User.Read", "Kullanıcı kayıtlarını görüntüleme", "Identity"),
        new("User.Create", "Yeni kullanıcı oluşturma", "Identity"),
        new("User.Update", "Kullanıcı bilgilerini güncelleme", "Identity"),
        new("User.Delete", "Kullanıcı silme (soft)", "Identity"),
        new("User.Lockout", "Kullanıcı kilitleme/açma", "Identity"),
        new("User.ResetPassword", "Kullanıcı şifresi sıfırlama (sistem onayı)", "Identity"),
        new("Role.Read", "Rol tanımlarını görüntüleme", "Identity"),
        new("Role.Create", "Yeni rol oluşturma", "Identity"),
        new("Role.Update", "Rol güncelleme", "Identity"),
        new("Role.Delete", "Rol silme", "Identity"),
        new("Role.AssignPermissions", "Role permission atama/kaldırma", "Identity"),
        new("Permission.Read", "Permission kataloğunu görüntüleme", "Identity"),

        // ---------- Tenant ----------
        new("Tenant.Read", "Tenant kayıtlarını görüntüleme", "Tenant"),
        new("Tenant.Create", "Yeni tenant onboarding", "Tenant"),
        new("Tenant.Update", "Tenant bilgilerini güncelleme", "Tenant"),
        new("Tenant.Suspend", "Tenant askıya alma", "Tenant"),
        new("Tenant.Terminate", "Tenant kapatma", "Tenant"),

        // ---------- Company ----------
        new("Company.Read", "Şirket kayıtlarını görüntüleme", "Company"),
        new("Company.Create", "Yeni şirket oluşturma", "Company"),
        new("Company.Update", "Şirket güncelleme", "Company"),
        new("Company.Delete", "Şirket silme", "Company"),

        // ---------- Building & Unit ----------
        new("Building.Read", "Bina/blok görüntüleme", "Site"),
        new("Building.Create", "Bina/blok oluşturma", "Site"),
        new("Building.Update", "Bina/blok güncelleme", "Site"),
        new("Building.Delete", "Bina/blok silme", "Site"),
        new("Unit.Read", "Bağımsız bölüm görüntüleme", "Site"),
        new("Unit.Create", "Bağımsız bölüm oluşturma", "Site"),
        new("Unit.Update", "Bağımsız bölüm güncelleme", "Site"),

        // ---------- Finans / Fatura / Ödeme ----------
        new("Invoice.Read", "Fatura görüntüleme", "Billing"),
        new("Invoice.Create", "Fatura oluşturma", "Billing"),
        new("Invoice.Approve", "Fatura onaylama", "Billing"),
        new("Invoice.Cancel", "Fatura iptali", "Billing"),
        new("Payment.Read", "Ödeme görüntüleme", "Billing"),
        new("Payment.Refund", "Ödeme iadesi", "Billing"),

        // ---------- Audit & Log ----------
        new("Audit.Read.Tenant", "Kendi tenant audit kayıtlarını görüntüleme", "Audit"),
        new("Audit.Read.All", "Tüm tenant'ların audit kayıtlarına erişim", "Audit"),
        new("Log.Read.Tenant", "Kendi tenant loglarını görüntüleme", "Audit"),
        new("Log.Read.All", "Tüm log kayıtlarına erişim", "Audit"),

        // ---------- Support Mode ----------
        new("Support.EnterMode", "Support Mode başlatma yetkisi", "Support"),
        new("Support.ElevateToWrite", "Support Mode'da yazma yetkisine yükselme", "Support"),
        new("Support.Impersonate", "Tenant kullanıcısı olarak görüntüleme (true impersonation)", "Support"),
        new("Support.History.Tenant", "Tenant'ın kendi destek erişim geçmişini görüntüleme", "Support"),

        // ---------- Reporting ----------
        new("Reporting.Tenant", "Tenant raporları", "Reporting"),
        new("Reporting.Company", "Şirket raporları", "Reporting"),
        new("Reporting.System", "Sistem geneli raporlar (System rol)", "Reporting"),

        // ---------- System ----------
        new("System.Settings.Manage", "Sistem ayarlarını yönetme", "System"),
        new("System.Localization.Manage", "Dil kaynaklarını yönetme", "System"),
    ];
}
