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
    /// <para>
    /// Taksitli — başlangıç/bitiş ayı + periyot (1-12 ay) ile <c>BudgetLineInstallment</c>
    /// satırları üretilir. Her taksit kendi tutarına sahiptir.
    /// </para>
    /// <para>
    /// Yatırım/Kuruluş bütçeleri için kullanılır. Mevsimsel giderler (Kömür) de
    /// aktif aylar için birer taksit satırı olarak modellenir (v0.2.14 — Seasonal
    /// bu tipe taşındı).
    /// </para>
    /// <para>
    /// <b>Manuel düzenleme:</b> <c>DistributionModel = Equal</c> ise her taksit
    /// tutarı elle değiştirilebilir (sum = PlannedAmount). m²/Arsa Payı dağılımda
    /// taksitler otomatik eşit bölünür ve elle değiştirilemez.
    /// </para>
    /// </summary>
    Installment = 3
}
