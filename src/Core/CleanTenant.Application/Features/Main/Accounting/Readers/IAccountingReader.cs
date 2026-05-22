namespace CleanTenant.Application.Features.Main.Accounting.Readers;

/// <summary>
/// <para>
/// Muhasebe raporlama okuyucu — Dapper tabanlı denormalize sorguların soyutlaması.
/// Implementasyon Faz 6'da <c>Infrastructure.Persistence</c> katmanında gelecek.
/// </para>
/// <para>
/// Tüm metodlar <c>companyId</c> bazlıdır; multi-tenancy izolasyonu
/// implementasyon katmanında SQL filtreleri ile sağlanır.
/// </para>
/// </summary>
public interface IAccountingReader
{
    /// <summary>Mizan raporu — hesap bazlı borç/alacak/bakiye özeti.</summary>
    Task<TrialBalanceReport> GetTrialBalanceAsync(
        Guid companyId,
        Guid fiscalYearId,
        int? month,
        CancellationToken ct);

    /// <summary>Büyük defter — belirli hesap kodu için tarih aralıklı hareketler.</summary>
    Task<IReadOnlyList<GeneralLedgerEntry>> GetGeneralLedgerAsync(
        Guid companyId,
        string accountCode,
        DateOnly from,
        DateOnly to,
        CancellationToken ct);

    /// <summary>Bilanço — belirli bir tarihe göre aktif/pasif dengesi.</summary>
    Task<BalanceSheetReport> GetBalanceSheetAsync(
        Guid companyId,
        DateOnly asOf,
        CancellationToken ct);

    /// <summary>Gelir tablosu — tarih aralığı gelir/gider/net kâr özeti.</summary>
    Task<IncomeStatementReport> GetIncomeStatementAsync(
        Guid companyId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct);

    /// <summary>Hesap ekstresi — hesap bazlı tarih aralıklı hareket listesi.</summary>
    Task<IReadOnlyList<AccountStatementEntry>> GetAccountStatementAsync(
        Guid companyId,
        string accountCode,
        DateOnly from,
        DateOnly to,
        CancellationToken ct);

    /// <summary>KDV özeti — belirli ay için indirilecek/hesaplanan/ödenecek KDV.</summary>
    Task<VatSummaryReport> GetVatSummaryAsync(
        Guid companyId,
        int year,
        int month,
        CancellationToken ct);

    /// <summary>Maliyet merkezi raporu — merkez ve hesap bazlı gider dağılımı.</summary>
    Task<IReadOnlyList<CostCenterReportEntry>> GetCostCenterReportAsync(
        Guid companyId,
        Guid? costCenterId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct);

    /// <summary>Bütçe-gerçekleşme karşılaştırması — mali yıl + ay bazlı sapma analizi.</summary>
    Task<BudgetVsActualReport> GetBudgetVsActualAsync(
        Guid companyId,
        Guid fiscalYearId,
        int? month,
        CancellationToken ct);

    /// <summary>Kasa/banka defteri — belirli nakit hesabının hareket listesi.</summary>
    Task<IReadOnlyList<CashBookEntry>> GetCashBookAsync(
        Guid companyId,
        string accountCode,
        DateOnly from,
        DateOnly to,
        CancellationToken ct);
}
