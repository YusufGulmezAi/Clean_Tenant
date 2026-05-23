using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Yevmiye satırı — <see cref="JournalEntry"/> fişinin tek bir
/// borç veya alacak kalemi. Her satır bir hesap koduna bağlanır;
/// aynı satırda ya <see cref="Debit"/> ya da <see cref="Credit"/> sıfır
/// olmayan değer taşır (ikisi birden sıfırdan büyük olamaz).
/// </para>
/// <para>
/// <b>Denormalize hesap kodu:</b> <see cref="AccountCodeValue"/> alanı
/// rapor sorgularının join maliyetini düşürmek için dönem anındaki
/// hesap kodu değerini saklar. Gerçek FK bağlantısı <see cref="AccountCodeId"/>
/// üzerindedir.
/// </para>
/// <para>
/// <b>Döviz:</b> <see cref="OriginalCurrency"/> dolu ise tutar dövizlidir;
/// <see cref="ExchangeRate"/> ile TRY karşılığı hesaplanır.
/// </para>
/// </summary>
public sealed class JournalLine : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Bağlı olduğu yevmiye fişi.</summary>
    public Guid JournalEntryId { get; set; }

    /// <summary>Satırın ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Borçlandırılan/alacaklandırılan hesap kodunun kimliği.</summary>
    public Guid AccountCodeId { get; set; }

    /// <summary>
    /// Denormalize hesap kodu değeri (örn. "100.001.001").
    /// Rapor performansı için join'i ortadan kaldırır;
    /// kayıt anındaki değeri yansıtır.
    /// </summary>
    public string AccountCodeValue { get; set; } = string.Empty;

    /// <summary>Borç tutarı (TRY). Alacak satırı ise 0.</summary>
    public decimal Debit { get; set; }

    /// <summary>Alacak tutarı (TRY). Borç satırı ise 0.</summary>
    public decimal Credit { get; set; }

    /// <summary>Opsiyonel satır açıklaması.</summary>
    public string? Description { get; set; }

    /// <summary>Bağlı maliyet merkezi (opsiyonel).</summary>
    public Guid? CostCenterId { get; set; }

    /// <summary>Proje kimliği (gelecek Faz); opsiyonel.</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Vergi kodu (örn. "KDV%20", "STOPAJ%20", "NOTVAT").
    /// e-Defter ve KDV beyanname entegrasyonunda kullanılır.
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>İlgili bağımsız bölüm (opsiyonel); aidat/gider dağıtım senaryoları için.</summary>
    public Guid? UnitId { get; set; }

    /// <summary>Dövizli işlemlerde orijinal tutar.</summary>
    public decimal? OriginalAmount { get; set; }

    /// <summary>ISO 4217 para birimi kodu (örn. "USD", "EUR"); TRY ise null.</summary>
    public string? OriginalCurrency { get; set; }

    /// <summary>Döviz kuru (1 birim yabancı para = X TRY); TRY işlemde null.</summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>Bağlı yevmiye fişi (navigation property).</summary>
    public JournalEntry JournalEntry { get; set; } = default!;
}
