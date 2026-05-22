using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// <b>Geçici (legacy) bütçe kayıt entity'si</b> — aylık ve hesap kodu
/// granülaritesinde planlanan tutar tutar. Her satır bir muhasebe dönemi
/// (<see cref="AccountingPeriodId"/>) + detay hesap kodu (<see cref="AccountCodeId"/>) +
/// opsiyonel maliyet merkezi (<see cref="CostCenterId"/>) kombinasyonunu temsil
/// eder.
/// </para>
/// <para>
/// <b>NOT (v0.2.13.a):</b> Bu entity, FAZ 5'te tanıtılan yeni
/// <see cref="CleanTenant.Domain.Tenant.Budgeting.Budget"/> aggregate'ine
/// taşıma sürecindedir. Şu anda geçici <c>TenantArea/Budget/BudgetPage.razor</c>
/// tarafından kullanılır; FAZ 5 Slice 4d ile birlikte kaldırılacak.
/// </para>
/// <para>
/// <b>Unique kısıtı:</b>
/// CompanyId + AccountingPeriodId + AccountCodeId + CostCenterId kombinasyonu
/// benzersizdir (DB tablosu hâlâ <c>budgets</c>; ad değişikliği sadece kod tarafı).
/// </para>
/// </summary>
public sealed class BudgetEntry : BaseEntity, ITenantScoped
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
