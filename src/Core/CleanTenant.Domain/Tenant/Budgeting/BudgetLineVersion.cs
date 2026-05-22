using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// Bir <see cref="BudgetVersion"/> kapsamında belirli bir <see cref="BudgetLine"/>
/// için planlanan tutar, ödeme planı ve dağıtım modeli. Yayınlanan versiyon
/// içinde immutable'dir; revizyon yapılırsa yeni versiyon altında yeni satır
/// oluşur (eski satır eski versiyonla kalır).
/// </para>
/// <para>
/// (BudgetVersionId, BudgetLineId) çifti benzersizdir; bir versiyonda aynı kalem
/// iki kez bulunmaz.
/// </para>
/// <para>
/// <b>Tahakkuk üretimi (FAZ 6):</b> <see cref="PaymentSchedule"/> tahakkuk
/// edilecek ayları, <see cref="DistributionModel"/> ise BB'lere dağıtım yöntemini
/// belirler. <see cref="DistributionConfig"/> JSON formatında ekstra parametre
/// taşır (örn. Seasonal için aktif aylar, Formula için formül metni).
/// </para>
/// </summary>
public sealed class BudgetLineVersion : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bağlı olduğu bütçe versiyonu (V1, V2, ...).</summary>
    public Guid BudgetVersionId { get; set; }

    /// <summary>Bağlı olduğu kalem tanımı.</summary>
    public Guid BudgetLineId { get; set; }

    /// <summary>
    /// Planlanan tutar. <see cref="PaymentSchedule.MonthlyEqual"/> için yıllık
    /// toplam, <see cref="PaymentSchedule.AnnualLumpSum"/> için tek seferlik tutar.
    /// </summary>
    public decimal PlannedAmount { get; set; }

    /// <summary>Tahakkuk takvimi (aylık eşit / yıllık / fatura / taksitli).</summary>
    public PaymentSchedule PaymentSchedule { get; set; } = PaymentSchedule.MonthlyEqual;

    /// <summary>BB'lere dağıtım modeli (eşit/m²/...).</summary>
    public DistributionModel DistributionModel { get; set; } = DistributionModel.Equal;

    /// <summary>
    /// Katılım grubu — bu kalem yalnızca grup üyesi BB'lere dağıtılır.
    /// Null ise sitedeki tüm aktif BB'lere dağıtım yapılır (KMK m.18 default).
    /// </summary>
    public Guid? ParticipationGroupId { get; set; }

    /// <summary>
    /// Dağıtım/plan ek parametreleri (JSON). Örn. Formula için formül metni.
    /// Null ise default davranış. (Seasonal v0.2.14'te Installment'a taşındı.)
    /// </summary>
    public string? DistributionConfig { get; set; }

    // ── Installment (PaymentSchedule == Installment için) ────────────────────

    /// <summary>Taksit planı başlangıç yılı. <see cref="PaymentSchedule.Installment"/> dışında null.</summary>
    public int? InstallmentStartYear { get; set; }

    /// <summary>Taksit planı başlangıç ayı (1-12).</summary>
    public int? InstallmentStartMonth { get; set; }

    /// <summary>Taksit planı bitiş yılı.</summary>
    public int? InstallmentEndYear { get; set; }

    /// <summary>Taksit planı bitiş ayı (1-12).</summary>
    public int? InstallmentEndMonth { get; set; }

    /// <summary>Taksit periyodu (ay): 1=aylık, 2=iki aylık, 3=üç aylık … 12=yıllık.</summary>
    public int? InstallmentIntervalMonths { get; set; }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bir önceki versiyondan kopyalanmadıysa (yeni eklenmiş veya manuel override
    /// edilmişse) true. UI'de bilgi amaçlı; mantıksal davranışı etkilemez.
    /// </summary>
    public bool IsManualOverride { get; set; }

    /// <summary>Manuel override sebebi. <see cref="IsManualOverride"/> = true ise dolu.</summary>
    public string? OverrideReason { get; set; }

    /// <summary>
    /// Tahakkuk vade günü (ayın 1-31 günü). Varsayılan 15. Ayda 31 olmayan günler
    /// için ayın son günü kullanılır (FAZ 6 mantığı). "Her ayın 15'i" gibi vade
    /// kuralları bu alan ile karşılanır.
    /// </summary>
    public int DueDayOfMonth { get; set; } = 15;

    /// <summary>Taksit satırları (yalnız <see cref="PaymentSchedule.Installment"/> için).</summary>
    public ICollection<BudgetLineInstallment> Installments { get; set; } = [];
}
