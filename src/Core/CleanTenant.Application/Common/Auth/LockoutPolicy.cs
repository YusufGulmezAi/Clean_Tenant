namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Bir kullanıcı için geçerli hesap kilitleme politikası — login akışında
/// hatalı şifre denemelerinin nasıl ele alınacağını belirler.
/// </para>
/// <para>
/// Politika tenant-başına ayarlanabilir (Catalog DB <c>tenants</c> kolonları);
/// System kullanıcıları ve çok-tenant'lı kullanıcılar için <see cref="Default"/>
/// geçerlidir. Çözümleme <see cref="IAccountLockoutService.ResolvePolicyAsync"/>
/// ile yapılır.
/// </para>
/// </summary>
/// <param name="Enabled">Kilitleme aktif mi? False ise hatalı denemeler sayılır
/// ama hesap hiç kilitlenmez.</param>
/// <param name="MaxFailedAttempts">Kilit için gereken ardışık hatalı deneme sayısı.</param>
/// <param name="Duration">Kilit süresi.</param>
public sealed record LockoutPolicy(bool Enabled, int MaxFailedAttempts, TimeSpan Duration)
{
    /// <summary>Global varsayılan: 5 deneme → 15 dakika (System/çok-tenant kullanıcılar).</summary>
    public static readonly LockoutPolicy Default =
        new(Enabled: true, MaxFailedAttempts: 5, Duration: TimeSpan.FromMinutes(15));
}
