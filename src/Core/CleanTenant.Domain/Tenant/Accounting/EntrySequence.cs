using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Fiş sıra numarası sayacı. Her şirket + mali yıl + fiş tipi kombinasyonu
/// için bağımsız bir sayaç tutar.
/// </para>
/// <para>
/// <b>Üretim:</b> Yeni fiş oluşturulurken <see cref="LastNumber"/> atomik
/// olarak artırılır (SELECT FOR UPDATE); üretilen numara
/// <see cref="JournalEntry.EntryNumber"/> alanına formatlanarak yazılır.
/// </para>
/// <para>
/// <b>Neden FiscalYearId?</b> Takvim dışı hesap dönemlerinde "yıl" belirsiz
/// olabileceğinden, sıra numarası takvim yılına değil mali yıl kaydına bağlanır.
/// </para>
/// </summary>
public sealed class EntrySequence : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Sayacın ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Sayacın bağlı olduğu mali yıl.</summary>
    public Guid FiscalYearId { get; set; }

    /// <summary>Fiş tipi — her tip kendi serisini bağımsız sayar.</summary>
    public EntryType EntryType { get; set; }

    /// <summary>Son üretilen sıra numarası; yeni fiş için +1 artırılır.</summary>
    public int LastNumber { get; set; }
}
