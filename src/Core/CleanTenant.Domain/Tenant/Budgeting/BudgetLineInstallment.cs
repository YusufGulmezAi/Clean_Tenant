using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// Taksit satırı — <see cref="BudgetLineVersion"/> içinde belirli bir (Yıl, Ay)
/// için tahakkuk edilecek <b>toplam</b> tutar (BB sayısına bölünmeden). Yalnız
/// <c>PaymentSchedule.Installment</c> kalemler için üretilir.
/// </para>
/// <para>
/// Tahakkuk üretiminde o ay için <see cref="Amount"/> alınır ve
/// <c>BudgetLineVersion.DistributionModel</c> ile BB'lere dağıtılır.
/// </para>
/// <para>
/// <b>Manuel düzenleme:</b> Yalnız <c>DistributionModel = Equal</c> ise
/// <see cref="Amount"/> kullanıcı tarafından değiştirilebilir (<see cref="IsManuallyEdited"/>);
/// m²/Arsa Payı dağılımda taksitler otomatik eşit bölünür ve elle değiştirilemez.
/// </para>
/// </summary>
public sealed class BudgetLineInstallment : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bağlı olduğu kalem versiyonu.</summary>
    public Guid BudgetLineVersionId { get; set; }

    /// <summary>Taksit sıra numarası (1, 2, 3 …) — UI sıralama + etiket.</summary>
    public int InstallmentNumber { get; set; }

    /// <summary>Taksit yılı.</summary>
    public int Year { get; set; }

    /// <summary>Taksit ayı (1-12).</summary>
    public int Month { get; set; }

    /// <summary>
    /// Bu taksit ayında tahakkuk edilecek TOPLAM tutar (BB'ye bölünmeden).
    /// SUM(Installment.Amount) = BudgetLineVersion.PlannedAmount olmalı.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>Opsiyonel etiket. Örn. "1. Taksit", "Avans".</summary>
    public string? Label { get; set; }

    /// <summary>
    /// Tutar kullanıcı tarafından elle düzeltildi mi (yalnız Equal modda mümkün).
    /// Bilgi amaçlı; m²/Arsa modda daima false.
    /// </summary>
    public bool IsManuallyEdited { get; set; }
}
