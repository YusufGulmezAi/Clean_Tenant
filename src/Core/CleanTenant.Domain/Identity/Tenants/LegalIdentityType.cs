namespace CleanTenant.Domain.Identity.Tenants;

/// <summary>
/// <para>
/// Bir <see cref="Tenant"/>'ın yasal kimlik tipi. Türkiye için 3 ayrı kimlik
/// modeli desteklenir; bir Tenant tam olarak <b>bir tanesini</b> taşır
/// (mutually exclusive). DB seviyesinde CHECK constraint ile format zorlanır.
/// </para>
/// </summary>
public enum LegalIdentityType
{
    /// <summary>Vergi Kimlik Numarası — 10 haneli, kurumsal yönetim (apartman tüzel kişiliği, profesyonel yönetim firması).</summary>
    Vkn = 1,

    /// <summary>T.C. Kimlik Numarası — 11 haneli, ilk hane 1-9 (gerçek kişi yönetim, şahıs adına kayıtlı yönetim).</summary>
    Tckn = 2,

    /// <summary>Yabancı Kimlik Numarası — 11 haneli, "99" ile başlar (yabancı sahipli/yönetilen yönetim).</summary>
    Ykn = 3,
}
