using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Şirkete ait banka hesabı — muhasebe modülü versiyonu.
/// LookUp katmanındaki <c>Bank</c> entity'sinden bağımsızdır; muhasebe
/// hesap planı (<see cref="AccountCodeId"/>) ile 102.xx kasa hesaplarına bağlanır.
/// </para>
/// <para>
/// <b>IBAN doğrulama:</b> TR + 24 hane formatı uygulama katmanı validator'ında
/// kontrol edilir; Domain bu kurala bağımlı değildir.
/// </para>
/// </summary>
public sealed class BankAccount : BaseEntity, IAggregateRoot, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Banka hesabının ait olduğu şirket.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Görünen ad (örn. "Garanti TRY Vadesiz").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Banka adı (örn. "Garanti BBVA", "İş Bankası").</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>Şube kodu (opsiyonel).</summary>
    public string? BranchCode { get; set; }

    /// <summary>Hesap numarası.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>IBAN numarası (TR + 24 hane); opsiyonel.</summary>
    public string? Iban { get; set; }

    /// <summary>Hesap türü: Checking / Savings / CreditLine.</summary>
    public BankAccountType AccountType { get; set; }

    /// <summary>ISO 4217 para birimi kodu; varsayılan "TRY".</summary>
    public string CurrencyCode { get; set; } = "TRY";

    /// <summary>
    /// Hesap planındaki karşılık hesap kimliği (102.xx grubu).
    /// Opsiyonel — eşleştirme daha sonra yapılabilir.
    /// </summary>
    public Guid? AccountCodeId { get; set; }

    /// <summary>Hesap aktif mi; pasif hesaplara yeni fiş yazılamaz.</summary>
    public bool IsActive { get; set; } = true;
}
