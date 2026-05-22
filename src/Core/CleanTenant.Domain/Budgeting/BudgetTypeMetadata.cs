using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Budgeting;

/// <summary>
/// <para>
/// Bütçe tipi sistem kataloğu (Catalog DB — System scope yönetir). Her
/// <see cref="BudgetType"/> için base muhasebe kodlarını ve varsayılan
/// davranışları tanımlar. Tenant'a özgü değildir; tüm yönetimler için ortaktır.
/// </para>
/// <para>
/// İlk tahakkuk anında <c>IAccountCodeAllocator</c> bu kataloğdan base kodları
/// (örn. Aidat → 120.01 / 600.01) okuyup şirkete özel alt hesap üretir
/// (120.01.001, 120.01.002 …).
/// </para>
/// </summary>
public sealed class BudgetTypeMetadata : BaseEntity
{
    /// <summary>Bütçe tipi (enum). Benzersiz.</summary>
    public BudgetType Type { get; set; }

    /// <summary>Görünen ad. Örn. "Aidat Bütçesi".</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 120 base alacak kodu (örn. "120.01"). İlk tahakkukta bunun altına seq eklenir.
    /// </summary>
    public string BaseReceivableCode { get; set; } = string.Empty;

    /// <summary>
    /// 600 base gelir kodu (örn. "600.01"). İlk tahakkukta bunun altına seq eklenir.
    /// </summary>
    public string BaseIncomeCode { get; set; } = string.Empty;

    /// <summary>
    /// Bu tip için varsayılan ödeme planı (UI'de ön-seçili gelir). Aidat → MonthlyEqual,
    /// Yatırım/Kuruluş/Kömür → Installment.
    /// </summary>
    public PaymentSchedule DefaultPaymentSchedule { get; set; }

    /// <summary>
    /// Bu tipten bir mali yılda birden fazla bütçe açılabilir mi? v0.2.14 — hepsi true.
    /// İleride bir tip için tekil zorunluluk gerekirse false yapılabilir.
    /// </summary>
    public bool AllowMultiplePerYear { get; set; } = true;

    /// <summary>UI sıralama önceliği.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Aktif mi; pasif tipler yeni bütçe oluştururken seçilemez.</summary>
    public bool IsActive { get; set; } = true;
}
