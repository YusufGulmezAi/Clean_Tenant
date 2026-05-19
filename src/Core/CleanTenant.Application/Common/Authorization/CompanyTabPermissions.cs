namespace CleanTenant.Application.Common.Authorization;

/// <summary>
/// Site (Company) detay sayfası tab'ları için permission code sabitleri.
/// Her tab için ayrı okuma/yazma yetkisi tanımlanır; UI tab'ı yalnız read
/// yetkisi varsa gösterir, Kaydet butonu yalnız write yetkisi varsa aktiftir.
/// Sistem (scope=System) kullanıcısı tüm yetkileri otomatik taşır (bypass).
/// </summary>
public static class CompanyTabPermissions
{
    /// <summary>Bilgiler tab — site adı, VKN, iletişim alanları (genel).</summary>
    public const string InfoRead = "company.info.read";

    /// <summary>Bilgiler tab — düzenleme yetkisi.</summary>
    public const string InfoWrite = "company.info.write";

    /// <summary>İletişim tab — adres, iletişim kanalları parametreleri.</summary>
    public const string ContactRead = "company.contact.read";

    /// <summary>İletişim tab — düzenleme yetkisi.</summary>
    public const string ContactWrite = "company.contact.write";

    /// <summary>Muhasebe tab — site'ye özel muhasebe parametreleri.</summary>
    public const string AccountingRead = "company.accounting.read";

    /// <summary>Muhasebe tab — düzenleme yetkisi.</summary>
    public const string AccountingWrite = "company.accounting.write";

    /// <summary>Finans tab — site'ye özel finans parametreleri.</summary>
    public const string FinanceRead = "company.finance.read";

    /// <summary>Finans tab — düzenleme yetkisi.</summary>
    public const string FinanceWrite = "company.finance.write";

    /// <summary>İK tab — site'ye özel İK parametreleri.</summary>
    public const string HrRead = "company.hr.read";

    /// <summary>İK tab — düzenleme yetkisi.</summary>
    public const string HrWrite = "company.hr.write";

    /// <summary>Puantaj tab — site'ye özel puantaj parametreleri.</summary>
    public const string TimesheetRead = "company.timesheet.read";

    /// <summary>Puantaj tab — düzenleme yetkisi.</summary>
    public const string TimesheetWrite = "company.timesheet.write";

    /// <summary>Bordro tab — site'ye özel bordro parametreleri.</summary>
    public const string PayrollRead = "company.payroll.read";

    /// <summary>Bordro tab — düzenleme yetkisi.</summary>
    public const string PayrollWrite = "company.payroll.write";

    /// <summary>Satınalma tab — site'ye özel satınalma parametreleri.</summary>
    public const string PurchasingRead = "company.purchasing.read";

    /// <summary>Satınalma tab — düzenleme yetkisi.</summary>
    public const string PurchasingWrite = "company.purchasing.write";
}
