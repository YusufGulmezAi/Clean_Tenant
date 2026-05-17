namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// 2FA login challenge'larını saklayan kısa ömürlü key-value store. Tipik
/// implementasyon Redis (<c>ct:2fa:challenge:{token}</c>) — TTL 5 dk.
/// </summary>
public interface ITwoFactorChallengeStore
{
    /// <summary>Challenge'ı saklar; TTL süresi sonunda otomatik silinir.</summary>
    Task StoreAsync(TwoFactorChallenge challenge, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>Token üzerinden challenge'ı okur; yoksa null.</summary>
    Task<TwoFactorChallenge?> GetAsync(Guid challengeToken, CancellationToken cancellationToken = default);

    /// <summary>Challenge'ı siler (replay engelleme: başarılı doğrulamadan hemen sonra çağrılır).</summary>
    Task DeleteAsync(Guid challengeToken, CancellationToken cancellationToken = default);
}
