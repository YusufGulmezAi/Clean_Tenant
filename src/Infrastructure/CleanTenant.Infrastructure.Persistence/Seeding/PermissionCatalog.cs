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
/// <b>Gruplama = SAYFA bazlı (karar 2026-05-24):</b> <c>Module</c> alanı, iznin
/// hangi ekrana/sayfaya ait olduğunu söyler (Rol Yönetimi ekranı izinleri buna
/// göre gruplar). Katalog, kodda gerçekten <c>[RequirePermission]</c> veya inline
/// <c>Permissions.Contains</c> ile zorlanan izinleri yansıtır — kullanılmayan
/// (orphan) kodlar bilinçli olarak listede tutulmaz.
/// </para>
/// <para>
/// <b>MinimumRoleScope (v0.2.8.a):</b> Privilege escalation önlemi için her
/// izin kodu "en az hangi scope'taki rolün tutabileceğini" taşır. Filter:
/// <c>role.Scope &lt;= permission.MinimumRoleScope</c>. Bkz:
/// <see cref="CleanTenant.Domain.Identity.Authorization.Permission.MinimumRoleScope"/>.
/// </para>
/// </summary>
public static class PermissionCatalog
{
    /// <summary>Permission tanımı (kod + açıklama + sayfa-grubu + minimum rol scope'u).</summary>
    /// <param name="Code">Stabil permission kodu.</param>
    /// <param name="Description">İnsan okunur açıklama.</param>
    /// <param name="Module">Sayfa-grubu (Rol Yönetimi ekranında gruplama başlığı).</param>
    /// <param name="MinimumRoleScope">Bu izni tutabilen en geniş rol scope'u.</param>
    public sealed record Definition(string Code, string Description, string Module, ScopeLevel MinimumRoleScope);

    /// <summary>Permission kataloğu — sayfa-grubuna göre düzenlenmiştir.</summary>
    public static readonly IReadOnlyList<Definition> All =
    [
        // ══════════ Erişim Yönetimi (/access — Kullanıcılar + Roller sekmeleri) ══════════
        new("User.Read",            "Kullanıcı kayıtlarını görüntüleme",          "AccessManagement", ScopeLevel.Tenant),
        new("User.Create",          "Yeni kullanıcı oluşturma",                   "AccessManagement", ScopeLevel.Tenant),
        new("User.Update",          "Kullanıcı bilgilerini güncelleme",           "AccessManagement", ScopeLevel.Tenant),
        new("User.Delete",          "Kullanıcı silme (soft)",                     "AccessManagement", ScopeLevel.Tenant),
        new("User.Lockout",         "Kullanıcı kilitleme/açma",                   "AccessManagement", ScopeLevel.Tenant),
        new("User.ResetPassword",   "Kullanıcı şifresi sıfırlama (sistem onayı)", "AccessManagement", ScopeLevel.System),
        new("System.Users.Manage",  "Sistem kullanıcılarını yönetme",            "AccessManagement", ScopeLevel.System),
        new("Tenant.Users.Manage",  "Yönetim (tenant) kullanıcılarını yönetme",  "AccessManagement", ScopeLevel.Tenant),
        new("Company.Users.Manage", "Site (company) kullanıcılarını yönetme",    "AccessManagement", ScopeLevel.Company),

        // ══════════ Yönetim (Tenant) Yönetimi (/system/tenants) ══════════
        new("Tenant.Read",          "Yönetim (tenant) kayıtlarını görüntüleme",  "TenantManagement", ScopeLevel.System),
        new("Tenant.Create",        "Yeni Yönetim onboarding",                   "TenantManagement", ScopeLevel.System),
        new("Tenant.Terminate",     "Yönetim kapatma",                           "TenantManagement", ScopeLevel.System),

        // ══════════ Site (Company) Yönetimi (/system|tenant/companies) ══════════
        new("Company.Create",       "Yeni site oluşturma",                       "CompanyManagement", ScopeLevel.Tenant),

        // ══════════ Site Detay Sekmeleri (CompanyDetailTabs — site parametreleri) ══════════
        new("company.info.read",        "Site bilgiler tab — okuma",       "CompanyDetail", ScopeLevel.Tenant),
        new("company.info.write",       "Site bilgiler tab — düzenleme",   "CompanyDetail", ScopeLevel.Tenant),
        new("company.contact.read",     "Site iletişim tab — okuma",       "CompanyDetail", ScopeLevel.Tenant),
        new("company.contact.write",    "Site iletişim tab — düzenleme",   "CompanyDetail", ScopeLevel.Tenant),
        new("company.accounting.read",  "Site muhasebe tab — okuma",       "CompanyDetail", ScopeLevel.Tenant),
        new("company.accounting.write", "Site muhasebe tab — düzenleme",   "CompanyDetail", ScopeLevel.Tenant),
        new("company.finance.read",     "Site finans tab — okuma",         "CompanyDetail", ScopeLevel.Tenant),
        new("company.finance.write",    "Site finans tab — düzenleme",     "CompanyDetail", ScopeLevel.Tenant),
        new("company.hr.read",          "Site İK tab — okuma",             "CompanyDetail", ScopeLevel.Tenant),
        new("company.hr.write",         "Site İK tab — düzenleme",         "CompanyDetail", ScopeLevel.Tenant),
        new("company.timesheet.read",   "Site puantaj tab — okuma",        "CompanyDetail", ScopeLevel.Tenant),
        new("company.timesheet.write",  "Site puantaj tab — düzenleme",    "CompanyDetail", ScopeLevel.Tenant),
        new("company.payroll.read",     "Site bordro tab — okuma",         "CompanyDetail", ScopeLevel.Tenant),
        new("company.payroll.write",    "Site bordro tab — düzenleme",     "CompanyDetail", ScopeLevel.Tenant),
        new("company.purchasing.read",  "Site satınalma tab — okuma",      "CompanyDetail", ScopeLevel.Tenant),
        new("company.purchasing.write", "Site satınalma tab — düzenleme",  "CompanyDetail", ScopeLevel.Tenant),

        // ══════════ Yapı Şeması (/building-schema — Ada/Parsel/Bina/BB) ══════════
        new("BuildingSchema.Read",   "Yapı şeması (ada/parsel/bina/BB) görüntüleme", "Definitions", ScopeLevel.Company),
        new("BuildingSchema.Manage", "Yapı şeması oluşturma/güncelleme/silme/içe aktarma", "Definitions", ScopeLevel.Company),

        // ══════════ Cari Kart (/tenant/current-accounts) ══════════
        new("tenant.party.view",          "Cari kart / kişi görüntüleme",                 "CurrentAccount", ScopeLevel.Tenant),
        new("tenant.party.edit",          "Cari kişi düzenleme",                          "CurrentAccount", ScopeLevel.Tenant),
        new("tenant.party.pii.view",      "PII (TCKN/VKN/telefon) maskesiz görme",        "CurrentAccount", ScopeLevel.Tenant),
        new("tenant.tenure.manage",       "BB-kişi tenure (malik/kiracı/hisse) yönetimi", "CurrentAccount", ScopeLevel.Tenant),
        new("tenant.currentaccount.view", "BB cari hareket defteri görüntüleme",          "CurrentAccount", ScopeLevel.Tenant),

        // ══════════ Bütçe (/company/budget) ══════════
        new("tenant.budget.view",             "Bütçe görüntüleme",        "Budget", ScopeLevel.Tenant),
        new("tenant.budget.edit",             "Bütçe taslak düzenleme",   "Budget", ScopeLevel.Tenant),
        new("tenant.budget.publish",          "Bütçe yayınlama / revize", "Budget", ScopeLevel.Tenant),
        new("tenant.budget.template.publish", "Bütçe şablonu paylaşma",   "Budget", ScopeLevel.Tenant),

        // ══════════ Tahakkuk & Tahsilat (/company/accruals, /collections) ══════════
        new("tenant.accrual.generate",   "Tahakkuk üretme",             "AccrualCollection", ScopeLevel.Tenant),
        new("tenant.collection.view",    "Tahsilat / borç görüntüleme", "AccrualCollection", ScopeLevel.Tenant),
        new("tenant.collection.record",  "Tahsilat kaydetme",           "AccrualCollection", ScopeLevel.Tenant),
        new("tenant.latefee.configure",  "Gecikme parametre yönetimi",  "AccrualCollection", ScopeLevel.Tenant),

        // ══════════ Muhasebe (/company/accounting/*) ══════════
        new("company.accounting.account-plan.read",  "Hesap planı görüntüleme",       "Accounting", ScopeLevel.Company),
        new("company.accounting.account-plan.write", "Hesap planı düzenleme",         "Accounting", ScopeLevel.Company),
        new("company.accounting.bank-account.read",  "Banka hesabı görüntüleme",      "Accounting", ScopeLevel.Company),
        new("company.accounting.bank-account.write", "Banka hesabı düzenleme",        "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.read",       "Yevmiye görüntüleme",           "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.write",      "Yevmiye fişi oluşturma",        "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.post",       "Yevmiye fişi kesinleştirme",    "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.approve",    "Yevmiye fişi onaylama",         "Accounting", ScopeLevel.Company),
        new("company.accounting.journal.void",       "Yevmiye fişi iptal etme",       "Accounting", ScopeLevel.Company),
        new("company.accounting.period.manage",      "Dönem yönetimi",                "Accounting", ScopeLevel.Company),
        new("company.accounting.period.override",    "Dönem kilidi kaldırma",         "Accounting", ScopeLevel.Tenant),
        new("company.accounting.invoice.read",       "Fatura görüntüleme",            "Accounting", ScopeLevel.Company),
        new("company.accounting.invoice.write",      "Fatura kaydetme",               "Accounting", ScopeLevel.Company),
        new("company.accounting.invoice.post",       "Fatura yevmiyeleştirme",        "Accounting", ScopeLevel.Company),
        new("company.accounting.reports.read",       "Rapor görüntüleme",             "Accounting", ScopeLevel.Company),
        new("company.accounting.settings.manage",    "Muhasebe ayarları",             "Accounting", ScopeLevel.Company),
        new("company.accounting.budget.read",        "Muhasebe bütçesi görüntüleme",  "Accounting", ScopeLevel.Company),
        new("company.accounting.budget.write",       "Muhasebe bütçesi düzenleme",    "Accounting", ScopeLevel.Company),

        // ══════════ Lokalizasyon (/system/localization) ══════════
        new("System.Localization.Manage", "Dil kaynaklarını yönetme", "Localization", ScopeLevel.System),

        // ══════════ Tanım Tabloları (/system/lookup-tables, /system/banks) ══════════
        new("LookUp.Manage", "Tanım tablolarını (il/ilçe/mahalle/mesken-yapı tipi/banka) yönetme", "LookUp", ScopeLevel.System),
    ];
}
