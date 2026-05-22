using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Budgeting;

/// <summary>
/// <para>
/// Yayınlanmış bütçe versiyonu — <see cref="Budget"/> aggregate'inin child entity'si.
/// Versiyon bir kez yayınlandığında geriye dönük güncellenmez (immutable).
/// </para>
/// <para>
/// <b>Geçerlilik penceresi:</b> <see cref="ValidFrom"/> dahil, <see cref="ValidTo"/>
/// dahil. <c>ValidTo</c> null ise versiyon açık uçludur (en güncel versiyon).
/// Revizyon yapıldığında eski versiyonun <c>ValidTo</c>'su yeni versiyonun
/// <c>ValidFrom - 1</c>'ine ayarlanır.
/// </para>
/// <para>
/// <b>Versiyon zinciri:</b> İleri yönlü linked list. V1.PreviousVersionId = null,
/// V2.PreviousVersionId = V1.Id, V3.PreviousVersionId = V2.Id. <see cref="VersionNumber"/>
/// kullanıcıya gösterilen sıra numarası.
/// </para>
/// <para>
/// <b>Tahakkuk bağlantısı:</b> FAZ 6'da üretilen <c>Accrual</c>'lar belirli bir
/// versiyonun <c>BudgetLineVersion</c>'larına bağlanır; versiyon arşivlense bile
/// tahakkuklar kaynak versiyonuyla iz sürülebilir kalır.
/// </para>
/// </summary>
public sealed class BudgetVersion : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Ait olduğu bütçenin id'si.</summary>
    public Guid BudgetId { get; set; }

    /// <summary>
    /// Kullanıcıya gösterilen versiyon sıra numarası: 1, 2, 3 …
    /// (BudgetId, VersionNumber) çifti benzersiz.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>Versiyon geçerlilik başlangıcı (dahil).</summary>
    public DateOnly ValidFrom { get; set; }

    /// <summary>
    /// Versiyon geçerlilik bitişi (dahil). Açık uçlu en güncel versiyonda null.
    /// Revizyon yapıldığında eski versiyonun bu alanı doldurulur.
    /// </summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>Önceki versiyonun id'si (V1 için null). Versiyon zincirini oluşturur.</summary>
    public Guid? PreviousVersionId { get; set; }

    /// <summary>Yayın tarihi (UTC).</summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>Yayınlayan kullanıcı kimliği; sistem işlemiyse null.</summary>
    public Guid? PublishedBy { get; set; }

    /// <summary>Revizyon gerekçesi. V1 (ilk yayın) için null; V2+ için zorunlu.</summary>
    public string? RevisionReason { get; set; }

    // ── Navigation (FAZ 5 Slice 2'de eklenecek) ──────────────────────────────
    // public ICollection<BudgetLineVersion> LineVersions { get; set; } = [];
}
