using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// Yıllık bütçe aggregate kökü — bir <see cref="Accounting.FiscalYear"/> için
/// bir adet (1 Site = 1 Bütçe; Karar #2 — 2026-05-22).
/// </para>
/// <para>
/// İçinde bir veya birden çok <see cref="BudgetVersion"/> bulunur. İlk yayınlama
/// V1'i oluşturur; yıl ortası revizyon yeni <see cref="BudgetVersion"/> (V2, V3 …)
/// üretir ve eski versiyonun <c>ValidTo</c>'su revizyon tarihinin bir gün öncesine
/// ayarlanır. Yayınlı versiyonlar geriye dönük güncellenmez; eski tahakkuklar
/// V1'e bağlı kalır, yeni dönem V2 üzerinden çalışır.
/// </para>
/// <para>
/// Erişim modeli: Bütçe verisi Company-scoped'tır, ancak UI/route/yetki Tenant
/// scope'a alınmıştır (v0.2.13.a Adım 0; Karar 2026-05-22). Tenant kullanıcısı
/// hangi sitenin bütçesinde çalışacağını seçer.
/// </para>
/// <para>
/// <b>Invariant'lar:</b>
/// <list type="bullet">
///   <item>(CompanyId, FiscalYearId) çifti benzersizdir (kısmi unique index, IsDeleted = false).</item>
///   <item>Status = Published iken en az bir <see cref="BudgetVersion"/> bulunmalıdır.</item>
///   <item>Versiyon zinciri ileri yönlüdür: <c>BudgetVersion.PreviousVersionId</c> daima daha eski bir versiyona işaret eder.</item>
/// </list>
/// </para>
/// </summary>
public sealed class Budget : BaseEntity, IAggregateRoot, ITenantScoped, IHasUrlCode
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bütçenin ait olduğu site (şirket). Veri Company-scoped'tır.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Yıllık bütçenin bağlı olduğu mali yıl. (CompanyId, FiscalYearId) çifti benzersiz.</summary>
    public Guid FiscalYearId { get; set; }

    /// <summary>9 karakterlik Base58 kısa kod; URL paylaşımı için.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Kullanıcı tarafından verilen başlık. Örn. "2026 Yıllık Bütçesi".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Açıklama / not. Opsiyonel.</summary>
    public string? Notes { get; set; }

    /// <summary>Bütçenin yaşam döngüsü durumu.</summary>
    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;

    /// <summary>
    /// Aktif yayınlı versiyonun id'si. Taslak iken null. Revizyon sonrası en son
    /// yayınlanan versiyona güncellenir; eski versiyonlar zincirde kalır.
    /// </summary>
    public Guid? CurrentVersionId { get; set; }

    /// <summary>Yıllık bütçenin tüm versiyonları (V1, V2, …).</summary>
    public ICollection<BudgetVersion> Versions { get; set; } = [];
}
