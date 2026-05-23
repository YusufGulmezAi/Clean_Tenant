using CleanTenant.Domain.Tenant.Collections.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Collections;

/// <summary>
/// <para>
/// Tahsilat — bir Bağımsız Bölümden alınan ödeme. Ödeme, vadesi en eski açık
/// tahakkuk detaylarına (TBK m.101: önce en eski borç) <see cref="Allocations"/>
/// üzerinden dağıtılır. Kısmi ödeme desteklenir (bir BB için N tahsilat).
/// </para>
/// <para>
/// Otomatik yevmiye fişi: Borç 100/102 (Kasa/Banka) / Alacak 120.0X.NNN
/// (Alıcılar) — tahakkukta açılan alacağı kapatır.
/// </para>
/// </summary>
public sealed class Collection : BaseEntity, IAggregateRoot, ITenantScoped, IHasUrlCode
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Tahsilatın ait olduğu site.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Ödemeyi yapan Bağımsız Bölüm.</summary>
    public Guid UnitId { get; set; }

    /// <summary>Tahsilatın muhasebeleştirildiği dönem.</summary>
    public Guid AccountingPeriodId { get; set; }

    /// <summary>9 karakterlik Base58 kısa kod (makbuz paylaşımı için).</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Ödeme tarihi.</summary>
    public DateOnly PaymentDate { get; set; }

    /// <summary>Tahsil edilen toplam tutar.</summary>
    public decimal Amount { get; set; }

    /// <summary>Ödeme yöntemi.</summary>
    public PaymentMethod Method { get; set; }

    /// <summary>Kasa/Banka hesap kodu (yevmiye borç tarafı). 100/102 yaprak hesabı.</summary>
    public Guid CashAccountCodeId { get; set; }

    /// <summary>Harici referans (banka dekont no, makbuz no). Opsiyonel.</summary>
    public string? Reference { get; set; }

    /// <summary>Açıklama. Opsiyonel.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Dağıtılamayan (avans/fazla ödeme) kısım. Açık borçtan fazla ödeme yapıldıysa
    /// burada tutulur; ileride yeni tahakkuklara mahsup edilebilir (Wave 2).
    /// </summary>
    public decimal UnallocatedAmount { get; set; }

    /// <summary>Otomatik açılan yevmiye fişi id'si.</summary>
    public Guid? JournalEntryId { get; set; }

    /// <summary>Kayıt anı (UTC).</summary>
    public DateTimeOffset RecordedAt { get; set; }

    /// <summary>Kaydeden kullanıcı.</summary>
    public Guid? RecordedBy { get; set; }

    /// <summary>Bu tahsilatın tahakkuk detaylarına dağıtımı.</summary>
    public ICollection<CollectionAllocation> Allocations { get; set; } = [];
}
