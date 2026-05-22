using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// Muafiyet kuralı — belirli bir Bağımsız Bölümün belirli bir bütçe kaleminden
/// tarih aralığında muaf tutulması. Örnek: yönetici dairesi temizlik aidatından
/// muaf; boş çıkmış bir BB belirli aylar elektrik aidatından muaf.
/// </para>
/// <para>
/// Tahakkuk üretimi sırasında dönem tarihine düşen aktif muafiyetler kontrol
/// edilir; muaf BB için <c>AccrualDetail</c> üretilmez (veya tutar 0 olur — FAZ 6
/// karar verecek).
/// </para>
/// <para>
/// <see cref="BudgetLineId"/>'ye bağlıdır (versiyon değil); kalem revize
/// edildiğinde muafiyet otomatik geçerli kalır. Belirli versiyona muafiyet
/// gerekirse Wave 2+'da ek alan eklenir.
/// </para>
/// </summary>
public sealed class ExemptionRule : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Kuralın ait olduğu site.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Muafiyet uygulanan Bağımsız Bölüm.</summary>
    public Guid UnitId { get; set; }

    /// <summary>Muafiyet kapsanan bütçe kalemi.</summary>
    public Guid BudgetLineId { get; set; }

    /// <summary>Muafiyet başlangıcı (dahil).</summary>
    public DateOnly ValidFrom { get; set; }

    /// <summary>Muafiyet bitişi (dahil). Açık uçluysa null.</summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>Muafiyet sebebi. KMK m.18 paylaşım kuralları gereği yazılı olmalı.</summary>
    public string Reason { get; set; } = string.Empty;
}
