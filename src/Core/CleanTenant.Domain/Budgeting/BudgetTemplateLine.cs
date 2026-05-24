using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Budgeting;

/// <summary>
/// <para>
/// Bütçe şablonu kalemi — <b>yapı-only</b> (tutar taşımaz). Şirkete özgü FK yerine
/// kategori/kalem/grup kod+ad'larını denormalize taşır; instantiate sırasında
/// hedef şirkette kod'a göre eşlenir veya oluşturulur.
/// </para>
/// </summary>
public sealed class BudgetTemplateLine : BaseEntity
{
    /// <summary>Bağlı olduğu şablon.</summary>
    public Guid BudgetTemplateId { get; set; }

    /// <summary>Gider kategorisi kodu (hedefte eşlenir/oluşturulur).</summary>
    public string CategoryCode { get; set; } = string.Empty;

    /// <summary>Gider kategorisi adı.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Üst kategori kodu (hiyerarşi); kök kategoride null.</summary>
    public string? ParentCategoryCode { get; set; }

    /// <summary>Bütçe kalemi kodu.</summary>
    public string LineCode { get; set; } = string.Empty;

    /// <summary>Bütçe kalemi adı.</summary>
    public string LineName { get; set; } = string.Empty;

    /// <summary>Kalem açıklaması. Opsiyonel.</summary>
    public string? LineDescription { get; set; }

    /// <summary>Ödeme planı (aylık eşit / yıllık / taksitli / fatura).</summary>
    public PaymentSchedule PaymentSchedule { get; set; }

    /// <summary>Dağıtım modeli (eşit / m² / ...).</summary>
    public DistributionModel DistributionModel { get; set; }

    /// <summary>Dağıtım/plan ek parametreleri (JSON). Opsiyonel.</summary>
    public string? DistributionConfig { get; set; }

    /// <summary>Tahakkuk vade günü (1-31).</summary>
    public int DueDayOfMonth { get; set; } = 15;

    /// <summary>Katılım grubu kodu (varsa hedefte boş grup oluşturulur). Opsiyonel.</summary>
    public string? ParticipationGroupCode { get; set; }

    /// <summary>Katılım grubu adı. Opsiyonel.</summary>
    public string? ParticipationGroupName { get; set; }

    /// <summary>Taksit periyodu (ay) — Installment kalemler için. Opsiyonel.</summary>
    public int? InstallmentIntervalMonths { get; set; }

    /// <summary>Taksit adedi (göreli; period-bağımsız) — Installment kalemler için. Opsiyonel.</summary>
    public int? InstallmentCount { get; set; }

    /// <summary>UI sıralama önceliği.</summary>
    public int DisplayOrder { get; set; }
}
