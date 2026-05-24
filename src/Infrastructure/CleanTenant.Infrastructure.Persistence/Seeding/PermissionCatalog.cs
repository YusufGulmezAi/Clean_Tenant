using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Infrastructure.Persistence.Seeding;

/// <summary>
/// <para>
/// CleanTenant'ta tanımlı tüm permission kodlarının statik kataloğu.
/// Format: <c>"&lt;Module&gt;.&lt;Action&gt;[.&lt;Qualifier&gt;]"</c>.
/// Yeni permission ekleyince listeyi güncellersin; <see cref="CatalogSeeder"/>
/// idempotent biçimde DB'ye işler (kod yoksa ekler, varsa metadata güncellenir).
/// </para>
/// <para>
/// <b>MinimumRoleScope (v0.2.8.a):</b> Privilege escalation önlemi için her
/// izin kodu "en az hangi scope'taki rolün tutabileceğini" taşır. Filter:
/// <c>role.Scope &lt;= permission.MinimumRoleScope</c>. Bkz:
/// <see cref="CleanTenant.Domain.Identity.Authorization.Permission.MinimumRoleScope"/>.
/// </para>
/// <para>
/// <b>Detaylı permission-rol haritalaması ManagementApp Rol Yönetimi ekranıyla yapılır;</b>
/// bu sınıf yalnız kod listesi + scope ceiling'i taşır.
/// </para>
/// </summary>
public static class PermissionCatalog
{
    /// <summary>Permission tanımı (kod + açıklama + modül + minimum rol scope'u).</summary>
    /// <param name="Code">Stabil permission kodu.</param>
    /// <param name="Description">İnsan okunur açıklama.</param>
    /// <param name="Module">Gruplama amaçlı modül adı.</param>
    /// <param name="MinimumRoleScope">Bu izni tutabilen en geniş rol scope'u.</param>
    public sealed record Definition(string Code, string Description, string Module, ScopeLevel MinimumRoleScope);

    /// <summary>Permission kataloğu — yeni eklemeler için liste sonuna ekle.</summary>
    public static readonly IReadOnlyList<Definition> All =
    [
        // ---------- Identity & Authorization ----------
        new("User.Read",            "Kullanıcı kayıtlarını görüntüleme", "Identity", ScopeLevel.Tenant),
        new("User.Create",          "Yeni kullanıcı oluşturma",          "Identity", ScopeLevel.Tenant),
        new("User.Update",          "Kullanıcı bilgilerini güncelleme",  "Identity", ScopeLevel.Tenant),
        new("User.Delete",          "Kullanıcı silme (soft)",            "Identity", ScopeLevel.Tenant),
        new("User.Lockout",         "Kullanıcı kilitleme/açma",          "Identity", ScopeLevel.Tenant),
        new("User.ResetPassword",   "Kullanıcı şifresi sıfırlama (sistem onayı)", "Identity", ScopeLevel.System),
        new("Role.Read",            "Rol tanımlarını görüntüleme",       "Identity", ScopeLevel.Tenant),
        new("Role.Create",          "Yeni rol oluşturma",                "Identity", ScopeLevel.Tenant),
        new("Role.Update",          "Rol güncelleme",                    "Identity", ScopeLevel.Tenant),
        new("Role.Delete",          "Rol silme",                         "Identity", ScopeLevel.Tenant),
        new("Role.AssignPermissions", "Role permission atama/kaldırma",  "Identity", ScopeLevel.Tenant),
        new("Permission.Read",      "Permission kataloğunu görüntüleme", "Identity", ScopeLevel.Tenant),
        new("Tenant.Users.Manage",  "Yönetim (tenant) kullanıcılarını yönetme", "Identity", ScopeLevel.Tenant),
        new("Company.Users.Manage", "Site (company) kullanıcılarını yönetme",   "Identity", ScopeLevel.Company),

        // ---------- Tenant (yalnız System) ----------
        new("Tenant.Read",          "Tenant kayıtlarını görüntüleme",    "Tenant", ScopeLevel.System),
        new("Tenant.Create",        "Yeni tenant onboarding",            "Tenant", ScopeLevel.System),
        new("Tenant.Update",        "Tenant bilgilerini güncelleme",     "Tenant", ScopeLevel.System),
        new("Tenant.Suspend",       "Tenant askıya alma",                "Tenant", ScopeLevel.System),
        new("Tenant.Terminate",     "Tenant kapatma",                    "Tenant", ScopeLevel.System),

        // ---------- Company ----------
        new("Company.Read",         "Şirket kayıtlarını görüntüleme",    "Company", ScopeLevel.Tenant),
        new("Company.Create",       "Yeni şirket oluşturma",             "Company", ScopeLevel.Tenant),
        new("Company.Update",       "Şirket güncelleme",                 "Company", ScopeLevel.Tenant),
        new("Company.Delete",       "Şirket silme",                      "Company", ScopeLevel.Tenant),

        // ---------- Building & Unit ----------
        new("Building.Read",        "Bina/blok görüntüleme",             "Site", ScopeLevel.Company),
        new("Building.Create",      "Bina/blok oluşturma",               "Site", ScopeLevel.Company),
        new("Building.Update",      "Bina/blok güncelleme",              "Site", ScopeLevel.Company),
        new("Building.Delete",      "Bina/blok silme",                   "Site", ScopeLevel.Company),
        new("Unit.Read",            "Bağımsız bölüm görüntüleme",        "Site", ScopeLevel.Company),
        new("Unit.Create",          "Bağımsız bölüm oluşturma",          "Site", ScopeLevel.Company),
        new("Unit.Update",          "Bağımsız bölüm güncelleme",         "Site", ScopeLevel.Company),

        // ---------- Finans / Fatura / Ödeme ----------
        new("Invoice.Read",         "Fatura görüntüleme",                "Billing", ScopeLevel.Company),
        new("Invoice.Create",       "Fatura oluşturma",                  "Billing", ScopeLevel.Company),
        new("Invoice.Approve",      "Fatura onaylama",                   "Billing", ScopeLevel.Company),
        new("Invoice.Cancel",       "Fatura iptali",                     "Billing", ScopeLevel.Company),
        new("Payment.Read",         "Ödeme görüntüleme",                 "Billing", ScopeLevel.Company),
        new("Payment.Refund",       "Ödeme iadesi",                      "Billing", ScopeLevel.Company),

        // ---------- Audit & Log ----------
        new("Audit.Read.Tenant",    "Kendi tenant audit kayıtlarını görüntüleme", "Audit", ScopeLevel.Tenant),
        new("Audit.Read.All",       "Tüm tenant'ların audit kayıtlarına erişim",  "Audit", ScopeLevel.System),
        new("Log.Read.Tenant",      "Kendi tenant loglarını görüntüleme",         "Audit", ScopeLevel.Tenant),
        new("Log.Read.All",         "Tüm log kayıtlarına erişim",                 "Audit", ScopeLevel.System),

        // ---------- Support Mode (System ağırlıklı) ----------
        new("Support.EnterMode",        "Support Mode başlatma yetkisi",                    "Support", ScopeLevel.System),
        new("Support.ElevateToWrite",   "Support Mode'da yazma yetkisine yükselme",         "Support", ScopeLevel.System),
        new("Support.Impersonate",      "Tenant kullanıcısı olarak görüntüleme (true impersonation)", "Support", ScopeLevel.System),
        new("Support.History.Tenant",   "Tenant'ın kendi destek erişim geçmişini görüntüleme",  "Support", ScopeLevel.Tenant),

        // ---------- Reporting ----------
        new("Reporting.Tenant",     "Tenant raporları",                  "Reporting", ScopeLevel.Tenant),
        new("Reporting.Company",    "Şirket raporları",                  "Reporting", ScopeLevel.Company),
        new("Reporting.System",     "Sistem geneli raporlar (System rol)", "Reporting", ScopeLevel.System),

        // ---------- System (yalnız System) ----------
        new("System.Settings.Manage",       "Sistem ayarlarını yönetme",       "System", ScopeLevel.System),
        new("System.Localization.Manage",   "Dil kaynaklarını yönetme",        "System", ScopeLevel.System),
        new("System.Users.Manage",          "Sistem kullanıcılarını yönetme",  "System", ScopeLevel.System),

        // ---------- LookUp Tabloları ----------
        new("LookUp.Read",          "LookUp tabloları (il, ilçe, mahalle, mesken tipi, yapı tipi, banka) görüntüleme", "LookUp", ScopeLevel.Tenant),
        new("LookUp.Manage",        "LookUp tablolarını oluşturma, güncelleme ve silme", "LookUp", ScopeLevel.System),

        // ---------- Company Detay Tab'ları (v0.2.7+ — site parametreleri) ----------
        new("company.info.read",         "Site bilgiler tab — okuma",       "CompanyParams", ScopeLevel.Tenant),
        new("company.info.write",        "Site bilgiler tab — düzenleme",   "CompanyParams", ScopeLevel.Tenant),
        new("company.contact.read",      "Site iletişim tab — okuma",       "CompanyParams", ScopeLevel.Tenant),
        new("company.contact.write",     "Site iletişim tab — düzenleme",   "CompanyParams", ScopeLevel.Tenant),
        new("company.accounting.read",   "Site muhasebe tab — okuma",       "CompanyParams", ScopeLevel.Tenant),
        new("company.accounting.write",  "Site muhasebe tab — düzenleme",   "CompanyParams", ScopeLevel.Tenant),
        new("company.finance.read",      "Site finans tab — okuma",         "CompanyParams", ScopeLevel.Tenant),
        new("company.finance.write",     "Site finans tab — düzenleme",     "CompanyParams", ScopeLevel.Tenant),
        new("company.hr.read",           "Site İK tab — okuma",             "CompanyParams", ScopeLevel.Tenant),
        new("company.hr.write",          "Site İK tab — düzenleme",         "CompanyParams", ScopeLevel.Tenant),
        new("company.timesheet.read",    "Site puantaj tab — okuma",        "CompanyParams", ScopeLevel.Tenant),
        new("company.timesheet.write",   "Site puantaj tab — düzenleme",    "CompanyParams", ScopeLevel.Tenant),
        new("company.payroll.read",      "Site bordro tab — okuma",         "CompanyParams", ScopeLevel.Tenant),
        new("company.payroll.write",     "Site bordro tab — düzenleme",     "CompanyParams", ScopeLevel.Tenant),
        new("company.purchasing.read",   "Site satınalma tab — okuma",      "CompanyParams", ScopeLevel.Tenant),
        new("company.purchasing.write",  "Site satınalma tab — düzenleme",  "CompanyParams", ScopeLevel.Tenant),

        // ---------- Muhasebe Modülü (v0.3.x) ----------
        new("company.accounting.account-plan.read",  "Hesap planı görüntüleme",       "Accounting", ScopeLevel.Company),
        new("company.accounting.account-plan.write", "Hesap planı düzenleme",         "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.read",       "Yevmiye görüntüleme",           "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.write",      "Yevmiye fişi oluşturma",        "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.post",       "Yevmiye fişi kesinleştirme",    "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.approve",    "Yevmiye fişi onaylama",         "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.void",       "Yevmiye fişi iptal etme",       "Accounting", ScopeLevel.Company),
        new("company.accounting.period.manage",      "Dönem yönetimi",                "Accounting", ScopeLevel.Company),
        new("company.accounting.period.override",    "Dönem kilidi kaldırma",         "Accounting", ScopeLevel.Tenant),
        new("company.accounting.fiscal-year.manage", "Mali yıl yönetimi",             "Accounting", ScopeLevel.Company),
        new("company.accounting.bank-account.read",  "Banka hesabı görüntüleme",      "Accounting", ScopeLevel.Company),
        new("company.accounting.bank-account.write", "Banka hesabı düzenleme",        "Accounting", ScopeLevel.Company),
        new("company.accounting.invoice.read",       "Fatura görüntüleme",            "Accounting", ScopeLevel.Company),
        new("company.accounting.invoice.write",      "Fatura kaydetme",               "Accounting", ScopeLevel.Company),
        new("company.accounting.invoice.post",       "Fatura yevmiyeleştirme",        "Accounting", ScopeLevel.Company),
        new("company.accounting.reports.read",       "Rapor görüntüleme",             "Accounting", ScopeLevel.Company),
        new("company.accounting.cost-center.read",   "Maliyet merkezi görüntüleme",   "Accounting", ScopeLevel.Company),
        new("company.accounting.cost-center.write",  "Maliyet merkezi düzenleme",     "Accounting", ScopeLevel.Company),
        new("company.accounting.settings.manage",    "Muhasebe ayarları",             "Accounting", ScopeLevel.Company),

        // ── Bütçe Modülü (Tenant scope — site-level erişim yok) ──────────────────
        // v0.2.13.a — Karar 2026-05-22: Bütçe modülü Site scope'tan kaldırıldı.
        // Yönetim seviyesinde permission'a bağlı erişim; veri Company-scoped kalır
        // (Yönetim kullanıcısı hangi sitenin bütçesinde çalıştığını seçer).
        new("tenant.budget.view",                    "Bütçe görüntüleme",             "Budget",     ScopeLevel.Tenant),
        new("tenant.budget.edit",                    "Bütçe taslak düzenleme",        "Budget",     ScopeLevel.Tenant),
        new("tenant.budget.publish",                 "Bütçe yayınlama / revize",      "Budget",     ScopeLevel.Tenant),
        new("tenant.accrual.generate",               "Tahakkuk üretme",               "Budget",     ScopeLevel.Tenant),
        new("tenant.collection.view",                "Tahsilat / borç görüntüleme",   "Budget",     ScopeLevel.Tenant),
        new("tenant.collection.record",              "Tahsilat kaydetme",             "Budget",     ScopeLevel.Tenant),
        new("tenant.latefee.configure",              "Gecikme parametre yönetimi",    "Budget",     ScopeLevel.Tenant),
        new("tenant.budget.template.publish",        "Bütçe şablonu paylaşma",        "Budget",     ScopeLevel.Tenant),

        // ---------- Cari (Party) / Tenure (F0) ----------
        new("tenant.party.view",                     "Cari kart / kişi görüntüleme",  "Party",      ScopeLevel.Tenant),
        new("tenant.party.edit",                     "Cari kişi düzenleme",           "Party",      ScopeLevel.Tenant),
        new("tenant.tenure.manage",                  "BB-kişi tenure (malik/kiracı/hisse) yönetimi", "Party", ScopeLevel.Tenant),
        new("tenant.party.pii.view",                 "PII (TCKN/VKN/telefon) maskesiz görme", "Party", ScopeLevel.Tenant),
        new("tenant.currentaccount.view",            "BB cari hareket defteri görüntüleme", "Party", ScopeLevel.Tenant),
    ];
}
