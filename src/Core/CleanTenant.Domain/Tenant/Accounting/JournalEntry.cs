using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Yevmiye fişi — muhasebe sisteminin temel kayıt birimi.
/// Borç-alacak eşitliği (<see cref="TotalDebit"/> = <see cref="TotalCredit"/>)
/// uygulama katmanında <c>JournalEntryValidator</c> tarafından zorunlu kılınır.
/// </para>
/// <para>
/// <b>Numara formatı:</b> <see cref="EntryNumber"/> = "2026-2027/001234"
/// (<see cref="EntrySequence"/> üzerinden üretilir, mali yıl bazlı).
/// </para>
/// <para>
/// <b>Ters fiş:</b> <see cref="VoidedAt"/> dolu olduğunda yeni bir ters fiş
/// oluşturulur; bu fişin <see cref="OriginalEntryId"/>'si iptal edilen fişe bağlanır.
/// </para>
/// </summary>
public sealed class JournalEntry : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Fişin ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Fişin ilişkilendirildiği muhasebe dönemi.</summary>
    public Guid AccountingPeriodId { get; set; }

    /// <summary>Fişin muhasebe niteliği: Opening, Normal, Adjustment vb.</summary>
    public EntryType EntryType { get; set; }

    /// <summary>
    /// Fiş numarası — mali yıl bazlı sıra numarası.
    /// Format: "2026-2027/001234".
    /// </summary>
    public string EntryNumber { get; set; } = string.Empty;

    /// <summary>Fişin muhasebe tarihi.</summary>
    public DateOnly EntryDate { get; set; }

    /// <summary>Açıklama metni; zorunlu.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Harici referans (belge/sözleşme/fatura numarası gibi serbest metin).
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Kaynak sistemdeki ilgili kaydın kimliği (fatura, banka ekstresi vb.).
    /// Hangi tabloya baktığını <see cref="Reference"/> bağlamı belirler.
    /// </summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>Toplam borç tutarı; tüm <see cref="JournalLine"/> borçlarının toplamı.</summary>
    public decimal TotalDebit { get; set; }

    /// <summary>Toplam alacak tutarı; tüm <see cref="JournalLine"/> alacaklarının toplamı.</summary>
    public decimal TotalCredit { get; set; }

    /// <summary>Fişin yaşam döngüsü durumu: Draft → PendingApproval → Posted / Voided.</summary>
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;

    /// <summary>Fişin muhasebeleştirildiği an (UTC); Draft/PendingApproval'da null.</summary>
    public DateTimeOffset? PostedAt { get; set; }

    /// <summary>Muhasebeleştirme işlemini yapan kullanıcı.</summary>
    public Guid? PostedBy { get; set; }

    /// <summary>Dual-control akışında onay anı (UTC).</summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>Onaylayan kullanıcı.</summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>Fişin iptal edildiği an (UTC).</summary>
    public DateTimeOffset? VoidedAt { get; set; }

    /// <summary>İptal işlemini yapan kullanıcı.</summary>
    public Guid? VoidedBy { get; set; }

    /// <summary>İptal gerekçesi; <see cref="VoidedAt"/> dolu olduğunda zorunlu.</summary>
    public string? VoidReason { get; set; }

    /// <summary>
    /// Ters fiş bağlantısı — bu fiş bir iptal fişiyse, iptal ettiği
    /// orijinal fişin kimliği buraya yazılır.
    /// </summary>
    public Guid? OriginalEntryId { get; set; }

    /// <summary>
    /// e-Defter XML çıktısı (stub). Faz 3+ e-Defter entegrasyonuna kadar
    /// bu alan boş kalır.
    /// </summary>
    public string? EDefterXml { get; set; }

    /// <summary>Fişe ait satırlar (borç/alacak detayları).</summary>
    public ICollection<JournalLine> Lines { get; set; } = [];
}
