namespace CleanTenant.Domain.Tenant.Budgeting.Enums;

/// <summary>
/// <para>
/// Bütçe kaleminin tahakkuk takvimi (ödeme planı tipi). Tahakkuk üretim motoru
/// (FAZ 6, <c>UretTahakkukCommand</c>) bu değere göre kalemin o ayda tahakkuk
/// edilip edilmeyeceğini ve tutarın nasıl hesaplanacağını belirler.
/// </para>
/// </summary>
public enum PaymentSchedule
{
    /// <summary>
    /// Yıllık tutar 12 aya eşit dağıtılır; her ay yıllık/12 tutarında tahakkuk üretilir.
    /// Standart aylık aidat için kullanılır.
    /// </summary>
    MonthlyEqual = 0,

    /// <summary>
    /// Yıllık tutarın tamamı belirli bir ayda (varsayılan Ocak) tek seferde tahakkuk eder.
    /// Yıllık abonelikler, peşin ödenen sigortalar için kullanılır.
    /// </summary>
    AnnualLumpSum = 1,

    /// <summary>
    /// Tahakkuk fatura/manuel tetikleme ile başlatılır; otomatik üretilmez.
    /// Düzensiz aralıklı bakım, onarım gibi olay-bazlı giderler için.
    /// </summary>
    InvoiceBased = 2,

    /// <summary>
    /// Mevsimsel — yalnız belirli aylarda tahakkuk üretilir (örn. yaz aylarında havuz bakım).
    /// Aktif aylar BudgetLineVersion'daki konfigürasyon alanında saklanır.
    /// </summary>
    Seasonal = 3
}
