using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Accounting;

/// <summary>
/// <para>
/// Şirket muhasebe yapılandırması — <see cref="Companies.Company"/> ile
/// 1:1 ilişkilidir. Her şirket için yalnızca bir kayıt bulunur.
/// </para>
/// <para>
/// <b>Aktivasyon:</b> <see cref="IsActivated"/> = false olan şirketler
/// muhasebe modülünü görüntüleyemez ve fiş giremez.
/// </para>
/// <para>
/// <b>Dual-control:</b> <see cref="RequireApproval"/> = true ise her
/// yevmiye fişi muhasebeleştirilmeden önce ikinci bir kullanıcı onayı gerektirir.
/// </para>
/// </summary>
public sealed class AccountingSettings : BaseEntity, ITenantScoped
{
    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Ayarların ait olduğu şirket (1:1 unique).</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Muhasebe modülü bu şirket için aktif mi.</summary>
    public bool IsActivated { get; set; }

    /// <summary>
    /// İkili kontrol (dual-control) zorunlu mu?
    /// true = her fiş, farklı bir kullanıcı tarafından onaylanmadan postalanmaz.
    /// Varsayılan: false.
    /// </summary>
    public bool RequireApproval { get; set; }

    /// <summary>Varsayılan para birimi; ISO 4217 kodu. Varsayılan: "TRY".</summary>
    public string DefaultCurrency { get; set; } = "TRY";

    /// <summary>KDV beyanname periyodu: Monthly veya Quarterly.</summary>
    public VatPeriod VatPeriod { get; set; } = VatPeriod.Monthly;

    /// <summary>
    /// e-Defter entegrasyonu aktif mi? Faz 3+ özelliği;
    /// şimdilik yalnızca bayrak olarak saklanır.
    /// </summary>
    public bool EDefterEnabled { get; set; }
}
