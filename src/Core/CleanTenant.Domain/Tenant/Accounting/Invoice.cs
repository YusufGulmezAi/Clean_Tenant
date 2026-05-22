using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Fatura kaydı — muhasebe modülünün belge yönetim birimi.
/// Hem gelen (gider/alış) hem giden (gelir/satış) faturaları tek entity
/// üzerinden <see cref="InvoiceDirection"/> ile ayrıştırılır.
/// </para>
/// <para>
/// <b>Yevmiyeye aktarım:</b> <see cref="IsPostedToJournal"/> = true ve
/// <see cref="JournalEntryId"/> dolu olduğunda fatura muhasebeleştirilmiş
/// sayılır; tekrar fiş kesilemez.
/// </para>
/// <para>
/// <b>Cari hesap:</b> Müşteri/tedarikçi cari takibi bu entity'de tutulmaz;
/// ilerleyen fazlarda ayrı Counterparty aggregate'ı eklenir.
/// </para>
/// </summary>
public sealed class Invoice : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Faturanın ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Faturanın bağlı olduğu muhasebe dönemi.</summary>
    public Guid AccountingPeriodId { get; set; }

    /// <summary>Fatura numarası (belge üzerindeki numara).</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Fatura tarihi.</summary>
    public DateOnly InvoiceDate { get; set; }

    /// <summary>Son ödeme tarihi (opsiyonel).</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Akış yönü: Incoming (gelen/gider) veya Outgoing (giden/gelir).</summary>
    public InvoiceDirection Direction { get; set; }

    /// <summary>Karşı tarafın adı (serbest metin; cari hesap entegrasyonuna kadar).</summary>
    public string CounterpartyName { get; set; } = string.Empty;

    /// <summary>Karşı tarafın Vergi Kimlik No veya TC Kimlik No (opsiyonel).</summary>
    public string? CounterpartyTaxId { get; set; }

    /// <summary>Faturanın bağlandığı gider veya gelir hesabı.</summary>
    public Guid AccountCodeId { get; set; }

    /// <summary>KDV hariç tutar.</summary>
    public decimal SubTotal { get; set; }

    /// <summary>Uygulanan KDV oranı kategorisi.</summary>
    public VatCategory VatCategory { get; set; }

    /// <summary>Hesaplanan KDV tutarı.</summary>
    public decimal VatAmount { get; set; }

    /// <summary>KDV dahil toplam tutar (SubTotal + VatAmount).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Fatura yevmiyeye aktarılmış mı.</summary>
    public bool IsPostedToJournal { get; set; }

    /// <summary>Oluşturulan yevmiye fişinin kimliği; henüz aktarılmadıysa null.</summary>
    public Guid? JournalEntryId { get; set; }

    /// <summary>Opsiyonel not/açıklama.</summary>
    public string? Notes { get; set; }
}
