using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// Yıllık bütçe aggregate kökü. v0.2.14 — Karar 2026-05-22: bir <see cref="Accounting.FiscalYear"/>
/// için aynı <see cref="BudgetType"/> altında birden fazla bütçe olabilir (örn.
/// Ana Aidat + Mart Ek Aidat; Çatı Yatırımı + Eylül Asansör Yatırımı).
/// </para>
/// <para>
/// İçinde bir veya birden çok <see cref="BudgetVersion"/> bulunur. İlk yayınlama
/// V1'i oluşturur; yıl ortası revizyon yeni <see cref="BudgetVersion"/> (V2, V3 …)
/// üretir ve eski versiyonun <c>ValidTo</c>'su revizyon tarihinin bir gün öncesine
/// ayarlanır. Yayınlı versiyonlar geriye dönük güncellenmez.
/// </para>
/// <para>
/// Erişim modeli: Bütçe verisi Company-scoped'tır, ancak UI/route/yetki Tenant
/// scope'a alınmıştır (v0.2.13.a Adım 0). Tenant kullanıcısı hangi sitenin
/// bütçesinde çalışacağını seçer.
/// </para>
/// <para>
/// Hesap kodu otomasyonu: <see cref="ReceivableAccountCodeId"/> ve
/// <see cref="IncomeAccountCodeId"/> ilk tahakkuk üretimi anında otomatik
/// üretilir. <c>BudgetTypeMetadata.BaseReceivableCode</c> + sıradaki seq
/// kullanılır (örn. Aidat → 120.01.001).
/// </para>
/// <para>
/// <b>Invariant'lar:</b>
/// <list type="bullet">
///   <item>(CompanyId, FiscalYearId, Type, Title) çifti benzersizdir (kısmi unique index, IsDeleted = false).</item>
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

    /// <summary>Yıllık bütçenin bağlı olduğu mali yıl. (CompanyId, FiscalYearId, Type, Title) çifti benzersiz.</summary>
    public Guid FiscalYearId { get; set; }

    /// <summary>
    /// Bütçe tipi (Aidat / Yatırım / Kömür / Kuruluş). Yan defter ve hesap kodu
    /// üretimi bu tipe göre yapılır (Catalog DB <c>BudgetTypeMetadata</c> kataloğu).
    /// v0.2.14 — yıl içinde aynı tipte birden fazla bütçe olabilir (ek aidat, çoklu yatırım).
    /// </summary>
    public BudgetType Type { get; set; }

    /// <summary>9 karakterlik Base58 kısa kod; URL paylaşımı için.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Kullanıcı tarafından verilen başlık. Örn. "2026 Yıllık Bütçesi" / "2026 Mart Ek Aidat".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Açıklama / not. Opsiyonel.</summary>
    public string? Notes { get; set; }

    // ── Bütçe Geçerlilik Dönemi (takvim yılına bağlı değil) ──────────────────
    // Bütçe kendi penceresinde çalışır; mali yıl içinde ama farklı aralık olabilir
    // (örn. Temmuz-Aralık ek aidat, Nisan-Ekim yatırım). MonthlyEqual kalemler
    // PlannedAmount'u bu penceredeki ay sayısına böler.

    /// <summary>Bütçe başlangıç yılı.</summary>
    public int PeriodStartYear { get; set; }

    /// <summary>Bütçe başlangıç ayı (1-12).</summary>
    public int PeriodStartMonth { get; set; }

    /// <summary>Bütçe bitiş yılı.</summary>
    public int PeriodEndYear { get; set; }

    /// <summary>Bütçe bitiş ayı (1-12, dahil).</summary>
    public int PeriodEndMonth { get; set; }

    /// <summary>Bütçenin yaşam döngüsü durumu.</summary>
    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;

    /// <summary>
    /// Bu bütçeden üretilen tahakkukların borç sorumluluğu modu (F0 — Cari Kart).
    /// <c>TenantThenOwner</c>: tahakkuk döneminde kiracı varsa kiracı, yoksa malik.
    /// <c>OwnerOnly</c>: her zaman malik. Varsayılan TenantThenOwner.
    /// </summary>
    public Parties.Enums.ResponsibilityMode ResponsibilityMode { get; set; }
        = Parties.Enums.ResponsibilityMode.TenantThenOwner;

    /// <summary>
    /// Aktif yayınlı versiyonun id'si. Taslak iken null. Revizyon sonrası en son
    /// yayınlanan versiyona güncellenir; eski versiyonlar zincirde kalır.
    /// </summary>
    public Guid? CurrentVersionId { get; set; }

    /// <summary>
    /// 120 alt hesap kodu (örn. 120.01.001) — ilk tahakkuk üretiminde otomatik
    /// oluşturulur ve buraya FK yazılır. Taslak iken ve yayın sonrası ilk tahakkuk
    /// öncesi null.
    /// </summary>
    public Guid? ReceivableAccountCodeId { get; set; }

    /// <summary>
    /// 600 alt hesap kodu (örn. 600.01.001) — ilk tahakkuk üretiminde otomatik
    /// oluşturulur. Taslak iken null.
    /// </summary>
    public Guid? IncomeAccountCodeId { get; set; }

    /// <summary>Yıllık bütçenin tüm versiyonları (V1, V2, …).</summary>
    public ICollection<BudgetVersion> Versions { get; set; } = [];
}
