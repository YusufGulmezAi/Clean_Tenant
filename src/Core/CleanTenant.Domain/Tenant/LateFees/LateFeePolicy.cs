using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.LateFees;

/// <summary>
/// <para>
/// Gecikme faizi politikası — vadesi geçmiş borçlara uygulanacak aylık oran ve
/// kurallar. <b>Hiyerarşik:</b> <see cref="BudgetId"/> <c>null</c> ise şirket-geneli
/// varsayılan; dolu ise o bütçeye özel override. Çözümleme: önce bütçe override,
/// yoksa şirket varsayılanı (bkz. <c>ILateFeePolicyResolver</c>).
/// </para>
/// <para>
/// Oran KMK m.20 tavanı (aylık %5, <see cref="RegulatoryLimits.KmkM20MonthlyCapPercent"/>)
/// ile sınırlanır. MVP basit faiz kullanır; <see cref="IsCompound"/> ileride açılır.
/// </para>
/// </summary>
public sealed class LateFeePolicy : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Politikanın ait olduğu site.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Bütçe id'si. <c>null</c> = şirket-geneli varsayılan politika;
    /// dolu = o bütçeye özel override.
    /// </summary>
    public Guid? BudgetId { get; set; }

    /// <summary>Aylık gecikme oranı yüzde (örn. 3.00 = aylık %3). KMK tavanı ile sınırlanır.</summary>
    public decimal MonthlyRatePercent { get; set; }

    /// <summary>Bileşik faiz mi? MVP'de <c>false</c> (basit faiz); flag ileri kullanım için.</summary>
    public bool IsCompound { get; set; }

    /// <summary>Vade sonrası gecikme işlemeyen gün sayısı (ödemesiz dönem). ≥ 0.</summary>
    public int GraceDays { get; set; }

    /// <summary>Gecikme geliri hesabı (yevmiye alacak tarafı; yaprak hesap, örn. 642/649).</summary>
    public Guid IncomeAccountCodeId { get; set; }

    /// <summary>Politika aktif mi.</summary>
    public bool IsActive { get; set; } = true;
}
