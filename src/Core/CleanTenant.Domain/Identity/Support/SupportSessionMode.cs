namespace CleanTenant.Domain.Identity.Support;

/// <summary>
/// Bir System operatörünün tenant bağlamına girerken seçtiği destek modunun
/// yetki düzeyi.
/// </summary>
public enum SupportSessionMode
{
    /// <summary>
    /// Salt okunur (default). Operatör tenant verisini görür ama değişiklik yapamaz.
    /// </summary>
    ReadOnly = 1,

    /// <summary>
    /// Yazma yetkili. Operatör veride değişiklik yapabilir. "Sebep" alanı zorunlu;
    /// her write aksiyonu audit'e işlenir.
    /// </summary>
    WriteEnabled = 2,

    /// <summary>
    /// True impersonation. Operatör belirli bir tenant kullanıcısının kimliğine
    /// bürünür; o kullanıcı olarak görür ve davranır. <c>impersonatedBy</c>
    /// claim'i her zaman taşınır; banner kalıcıdır.
    /// </summary>
    FullImpersonation = 3,
}
