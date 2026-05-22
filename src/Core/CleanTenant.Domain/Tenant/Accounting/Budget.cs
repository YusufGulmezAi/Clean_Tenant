using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Bütçe kalemi — aylık ve hesap kodu granülaritesinde planlanan tutar.
/// Her satır bir muhasebe dönemi (<see cref="AccountingPeriodId"/>) +
/// detay hesap kodu (<see cref="AccountCodeId"/>) + opsiyonel maliyet
/// merkezi (<see cref="CostCenterId"/>) kombinasyonunu temsil eder.
/// </para>
/// <para>
/// <b>Unique kısıtı:</b>
/// CompanyId + AccountingPeriodId + AccountCodeId + CostCenterId kombinasyonu
/// benzersiz olmalıdır; persistence katmanında unique index ile sağlanır.
/// </para>
/// <para>
/// <b>Sapma analizi:</b> Gerçekleşen tutarlar <see cref="JournalLine"/>
/// sorgularıyla hesaplanır; bu entity yalnızca plan değerini taşır.
/// </para>
/// </summary>
public sealed class Budget : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bütçenin ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Hangi muhasebe dönemine (ay) ait bütçe kalemi.</summary>
    public Guid AccountingPeriodId { get; set; }

    /// <summary>
    /// Bütçelenen hesap kodu kimliği; yalnızca IsDetail = true hesaplar
    /// bütçelenebilir.
    /// </summary>
    public Guid AccountCodeId { get; set; }

    /// <summary>Maliyet merkezine göre ayrıştırma (opsiyonel).</summary>
    public Guid? CostCenterId { get; set; }

    /// <summary>Dönem için planlanan tutar (TRY).</summary>
    public decimal BudgetedAmount { get; set; }
}
