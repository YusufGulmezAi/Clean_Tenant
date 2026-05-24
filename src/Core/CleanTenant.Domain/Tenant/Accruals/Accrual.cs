using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accruals;

/// <summary>
/// <para>
/// Tahakkuk başlığı (header) — belirli bir dönem için üretilen borçlandırmanın
/// muhasebe-seviyesi özeti. Yevmiye fişi <b>bu seviyede</b> açılır (Bütçe × Dönem
/// = 1 fiş; Karar 2026-05-22). BB-bazlı kırılım <see cref="AccrualDetail"/>'de
/// (yardımcı defter) tutulur.
/// </para>
/// <para>
/// Kaynağa göre alanlar (<see cref="Source"/>):
/// <list type="bullet">
///   <item><b>Budget:</b> BudgetId + BudgetVersionId + AccountingPeriodId dolu.</item>
///   <item><b>Invoice:</b> InvoiceId dolu.</item>
///   <item><b>DirectCharge:</b> yalnız tek BB'ye; Details tek satır.</item>
/// </list>
/// </para>
/// <para>
/// <b>İdempotency (Karar B):</b> Budget kaynağı için (BudgetId, AccountingPeriodId)
/// benzersizdir. Aynı dönem ikinci kez üretilirse: tahsilat yoksa eski silinip
/// yenilenebilir (force), tahsilat varsa reddedilir.
/// </para>
/// </summary>
public sealed class Accrual : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Tahakkuğun ait olduğu site.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Tahakkuk kaynağı (Budget / Invoice / DirectCharge).</summary>
    public AccrualSource Source { get; set; }

    // ── Budget kaynağı ───────────────────────────────────────────────────────

    /// <summary>Bütçe id'si (Source = Budget için dolu).</summary>
    public Guid? BudgetId { get; set; }

    /// <summary>Tahakkuğun üretildiği bütçe versiyonu (Source = Budget).</summary>
    public Guid? BudgetVersionId { get; set; }

    /// <summary>Muhasebe dönemi (Source = Budget). Year/Month ile tutarlı.</summary>
    public Guid? AccountingPeriodId { get; set; }

    // ── Invoice kaynağı ────────────────────────────────────────────────────────

    /// <summary>Fatura id'si (Source = Invoice için dolu).</summary>
    public Guid? InvoiceId { get; set; }

    // ── Ortak ──────────────────────────────────────────────────────────────────

    /// <summary>Tahakkuk dönemi yılı (tüm kaynaklarda dolu).</summary>
    public int Year { get; set; }

    /// <summary>Tahakkuk dönemi ayı (1-12).</summary>
    public int Month { get; set; }

    /// <summary>BB detaylarının toplamı (yevmiye fişi tutarı).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Borç hesabı (120.0X.NNN) — yevmiye fişi borç tarafı.</summary>
    public Guid? ReceivableAccountCodeId { get; set; }

    /// <summary>Gelir hesabı (600.0X.NNN) — yevmiye fişi alacak tarafı.</summary>
    public Guid? IncomeAccountCodeId { get; set; }

    /// <summary>Otomatik açılan yevmiye fişi id'si (hesap kodları set ise dolu).</summary>
    public Guid? JournalEntryId { get; set; }

    /// <summary>Açıklama (fiş referansı, fatura no vb.).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Üretim anı (UTC).</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>Üreten kullanıcı; Hangfire/sistem işlemiyse null.</summary>
    public Guid? GeneratedBy { get; set; }

    /// <summary>
    /// Üretim anındaki sorumluluk modu (snapshot; bütçeden kopyalanır). Kaynağı
    /// olmayan eski tahakkuklarda null. Bkz. <see cref="Parties.Enums.ResponsibilityMode"/>.
    /// </summary>
    public Parties.Enums.ResponsibilityMode? ResponsibilityMode { get; set; }

    /// <summary>BB-bazlı tahakkuk detayları (yardımcı defter).</summary>
    public ICollection<AccrualDetail> Details { get; set; } = [];
}
