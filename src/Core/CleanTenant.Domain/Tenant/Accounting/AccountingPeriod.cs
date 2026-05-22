using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Muhasebe dönemi — <see cref="FiscalYear"/> altındaki aylık alt birim.
/// Yevmiye fişleri bu dönemle ilişkilendirilir; dönem kilitlendiğinde
/// yeni fiş girilemez.
/// </para>
/// <para>
/// <b>Takvim yılı dışı dönemler:</b> <see cref="Year"/> ve <see cref="Month"/>
/// takvim tarihini, <see cref="StartDate"/>/<see cref="EndDate"/> ise gerçek
/// gün aralığını gösterir. Raporlama bu iki alan arasında tutulur.
/// </para>
/// </summary>
public sealed class AccountingPeriod : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Dönemin ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Bağlı olduğu mali yıl.</summary>
    public Guid FiscalYearId { get; set; }

    /// <summary>Dönemin takvim yılı (örn. 2026).</summary>
    public int Year { get; set; }

    /// <summary>Dönemin takvim ayı (1–12).</summary>
    public int Month { get; set; }

    /// <summary>Dönemin başlangıç tarihi (dahil).</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Dönemin bitiş tarihi (dahil).</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Dönemin kilitlenme durumu: Open / Locked / ClosedPermanent.</summary>
    public PeriodStatus Status { get; set; } = PeriodStatus.Open;

    /// <summary>Bağlı mali yıl (navigation property).</summary>
    public FiscalYear FiscalYear { get; set; } = default!;
}
