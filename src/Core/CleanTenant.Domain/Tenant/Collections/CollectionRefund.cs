using CleanTenant.Domain.Tenant.Collections.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Collections;

/// <summary>
/// <para>
/// Avans (fazla ödeme) iadesi — sakine nakit geri ödeme. 120-negatif modelde avans,
/// BB'nin alacak hesabında (120) alacaklı/negatif bakiye olarak durur; iade bu bakiyeyi
/// borçlandırıp nakit çıkarır.
/// </para>
/// <para>
/// Otomatik yevmiye fişi: Borç 120.0X.NNN (<see cref="AdvanceAccountCodeId"/>) /
/// Alacak 100-102 (<see cref="CashAccountCodeId"/> — Kasa/Banka). Tahsilat (nakit girişi)
/// değildir; ayrı yaşam döngüsü + ayrı izin (<c>tenant.advance.refund</c>).
/// </para>
/// </summary>
public sealed class CollectionRefund : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>İadenin ait olduğu site.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>İade yapılan bağımsız bölüm.</summary>
    public Guid UnitId { get; set; }

    /// <summary>İade tutarı (&gt; 0).</summary>
    public decimal Amount { get; set; }

    /// <summary>İade tarihi.</summary>
    public DateOnly RefundDate { get; set; }

    /// <summary>Nakit çıkış hesabı (100 Kasa / 102 Banka, yaprak).</summary>
    public Guid CashAccountCodeId { get; set; }

    /// <summary>Borçlandırılan alacak hesabı (120 — avansın durduğu hesap).</summary>
    public Guid AdvanceAccountCodeId { get; set; }

    /// <summary>İade yöntemi (nakit/banka/çek).</summary>
    public PaymentMethod Method { get; set; }

    /// <summary>Referans / dekont no.</summary>
    public string? Reference { get; set; }

    /// <summary>Açıklama.</summary>
    public string? Description { get; set; }

    /// <summary>Oluşturulan yevmiye fişi.</summary>
    public Guid? JournalEntryId { get; set; }

    /// <summary>Kayıt anı (UTC).</summary>
    public DateTimeOffset RefundedAt { get; set; }

    /// <summary>Kaydı yapan kullanıcı.</summary>
    public Guid? RefundedBy { get; set; }
}
