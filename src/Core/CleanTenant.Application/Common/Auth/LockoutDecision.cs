namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// Hesap kilitleme kararlarının <b>saf (in-memory, DB'siz)</b> mantığı. Test
/// edilebilirlik için <see cref="AccountLockoutService"/>'ten ayrıştırıldı —
/// servis yalnız DB/Identity orkestrasyonunu yapar, kararı buraya sorar.
/// </summary>
public static class LockoutDecision
{
    /// <summary>
    /// Kullanıcının bağlı olduğu tenant'ların politikalarından etkin olanı seçer.
    /// Tam olarak bir tenant politikası varsa o uygulanır; aksi halde (System
    /// kullanıcı = 0 tenant, veya çok-tenant = birden çok) <see cref="LockoutPolicy.Default"/>.
    /// </summary>
    public static LockoutPolicy SelectEffective(IReadOnlyCollection<LockoutPolicy> tenantPolicies)
        => tenantPolicies.Count == 1 ? tenantPolicies.First() : LockoutPolicy.Default;

    /// <summary>
    /// Politika ve güncel (artırılmış) hatalı deneme sayısına göre hesabın bu
    /// denemede kilitlenmesi gerekip gerekmediğini söyler. Kilitleme kapalıysa
    /// her zaman <c>false</c>.
    /// </summary>
    public static bool ShouldLock(LockoutPolicy policy, int failedCount)
        => policy.Enabled && failedCount >= policy.MaxFailedAttempts;
}
