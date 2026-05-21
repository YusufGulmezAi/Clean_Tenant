namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// İlk giriş şifre değişimi challenge'larını saklayan kısa ömürlü key-value store.
/// Tipik implementasyon Redis (<c>ct:pwd-chg:{token}</c>) — TTL 15 dk.
/// </summary>
public interface IPasswordChangeChallengeStore
{
    /// <summary>Challenge'ı saklar; TTL süresi sonunda otomatik silinir.</summary>
    Task StoreAsync(PasswordChangeChallenge challenge, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>Token üzerinden challenge'ı okur; yoksa null.</summary>
    Task<PasswordChangeChallenge?> GetAsync(Guid challengeToken, CancellationToken cancellationToken = default);

    /// <summary>Challenge'ı siler (replay engelleme: tamamlandıktan sonra).</summary>
    Task DeleteAsync(Guid challengeToken, CancellationToken cancellationToken = default);
}
