namespace CleanTenant.Domain.Tenant.Accruals.Enums;

/// <summary>
/// <para>
/// Tahakkuğun kaynağı — tahakkuk hangi mekanizmadan üretildi. Yevmiye fişi
/// hesap kodu eşlemesi ve raporlama bu değere göre yapılır.
/// </para>
/// </summary>
public enum AccrualSource
{
    /// <summary>
    /// Bütçe-bazlı tahakkuk (UretTahakkukCommand). BudgetId + BudgetVersionId +
    /// AccountingPeriodId doludur. Kalem versiyonlarından toplam üretilir.
    /// </summary>
    Budget = 0,

    /// <summary>
    /// Fatura-bazlı dağıtımlı tahakkuk (DistributeInvoiceAmongUnitsCommand).
    /// InvoiceId doludur; tutar katılım grubu + dağıtım modeli ile BB'lere bölünür
    /// (örn. doğalgaz/su faturası ısınanlara m² oranlı).
    /// </summary>
    Invoice = 1,

    /// <summary>
    /// Doğrudan BB borçlandırma (CreateDirectUnitChargeCommand). Tek bir BB'ye
    /// dağıtımsız borç (örn. depo kira, site yönetiminden ürün satışı).
    /// </summary>
    DirectCharge = 2,

    /// <summary>
    /// Gecikme faizi tahakkuğu (GenerateLateFeeChargesCommand). Vadesi geçmiş açık
    /// anapara detayları üzerinden KMK m.20 tavanlı basit faiz. Borç 120.0X (anapara
    /// alacak hesabı) / Alacak gecikme-geliri. İdempotency partial index (source=0)
    /// kapsamı dışındadır; bir dönemde birden çok run serbesttir.
    /// </summary>
    LateFee = 3
}
