using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accruals;

/// <summary>
/// <para>
/// Tahakkuk detayı (yardımcı defter satırı) — bir <see cref="Accrual"/> içindeki
/// bir Bağımsız Bölümün payı. Yevmiyeye girmez; 120 alt hesabının BB-bazlı
/// kırılımını taşır. Borç durumu (BorcDurumu) bu satırlardan hesaplanır.
/// </para>
/// <para>
/// (AccrualId, UnitId) çifti benzersizdir.
/// </para>
/// </summary>
public sealed class AccrualDetail : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bağlı olduğu tahakkuk başlığı.</summary>
    public Guid AccrualId { get; set; }

    /// <summary>Borçlandırılan Bağımsız Bölüm.</summary>
    public Guid UnitId { get; set; }

    /// <summary>BB'nin bu tahakkuktaki toplam payı (TL).</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Dağıtım payı oranı (örn. m² oranı 0.0090, eşit dağılımda 1/N). Bilgi/denetim
    /// amaçlı; tutar zaten <see cref="Amount"/>'ta.
    /// </summary>
    public decimal DistributionShare { get; set; }

    /// <summary>Vade tarihi (DueDayOfMonth + dönem ayından hesaplanır).</summary>
    public DateOnly DueDate { get; set; }

    /// <summary>
    /// Kalem-bazlı kırılım (JSONB) — BB sahibinin "1.500 TL'nin breakdown'ı"
    /// sorusuna cevap. Örn. <c>[{"lineCode":"ASB-01","lineName":"Asansör Bakım","amount":100.00}]</c>.
    /// Yevmiyeye girmez; PortalApp şeffaflık ekranı için.
    /// </summary>
    public string? LineBreakdownJson { get; set; }

    // ── Sorumluluk (F0 — Cari Kart) ──────────────────────────────────────────

    /// <summary>
    /// Bu borcun birincil sorumlusu (denormalize — en uzun süreli/en yüksek paylı
    /// taraf). Hızlı liste/KPI/cari atıf için. Gerçek dağılım <see cref="Responsibilities"/>'te.
    /// </summary>
    public Guid? PrimaryResponsiblePartyId { get; set; }

    /// <summary>Sorumlu çözümleme notu (örn. "kiracı aktif", "boş dönem → malik").</summary>
    public string? ResponsibleResolvedNote { get; set; }

    /// <summary>
    /// Düzeltme (Correction) detayında, geri alınan ORİJİNAL tahakkuk detayına işaret eder
    /// (negatif <see cref="Amount"/> taşır). Normal detaylarda null. Aşırı-ters-kayıt
    /// kontrolü ve izlenebilirlik için.
    /// </summary>
    public Guid? CorrectedAccrualDetailId { get; set; }

    /// <summary>Gün-bazlı sorumluluk parçaları (Σ Amount = <see cref="Amount"/>).</summary>
    public ICollection<Parties.AccrualResponsibilitySplit> Responsibilities { get; set; } = [];
}
