using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Mali yıl — muhasebe dönemlerinin (<see cref="AccountingPeriod"/>) çatı
/// birimi. Takvim yılına (01.01–31.12) uymayan özel hesap dönemleri de
/// desteklenir (örn. 01.05.2026–30.04.2027).
/// </para>
/// <para>
/// <b>Cari yıl:</b> <see cref="IsCurrentYear"/> yalnızca bir kayıtta true
/// olmalıdır; uygulama katmanı bu kısıtı zorunlu kılar.
/// </para>
/// </summary>
public sealed class FiscalYear : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Mali yılın ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Görünen etiket. Takvim yılı için "2025"; özel dönem için "2026-2027".
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Mali yılın başlangıç tarihi (dahil). Takvim dışı dönem olabilir.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Mali yılın bitiş tarihi (dahil). Takvim dışı dönem olabilir.</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Dönemin kilitlenme durumu: Open / Locked / ClosedPermanent.</summary>
    public PeriodStatus Status { get; set; } = PeriodStatus.Open;

    /// <summary>Bu mali yıl cari (aktif) yıl mı.</summary>
    public bool IsCurrentYear { get; set; }

    /// <summary>Mali yıl içindeki muhasebe dönemleri (genellikle 12 ay).</summary>
    public ICollection<AccountingPeriod> Periods { get; set; } = [];
}
