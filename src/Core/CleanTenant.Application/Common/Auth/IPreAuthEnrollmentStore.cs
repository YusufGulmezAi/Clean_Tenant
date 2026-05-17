namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// Pre-auth 2FA enrollment challenge'larını saklayan kısa ömürlü key-value store.
/// Tipik implementasyon Redis (<c>ct:2fa:preauth-enroll:{token}</c>) — TTL 10 dk.
/// </summary>
public interface IPreAuthEnrollmentStore
{
    /// <summary>Challenge'ı saklar; TTL süresi sonunda otomatik silinir.</summary>
    Task StoreAsync(PreAuthEnrollmentChallenge challenge, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>Token üzerinden challenge'ı okur; yoksa null.</summary>
    Task<PreAuthEnrollmentChallenge?> GetAsync(Guid challengeToken, CancellationToken cancellationToken = default);

    /// <summary>Var olan challenge'ın <see cref="PreAuthEnrollmentChallenge.VerifiedAt"/> alanını günceller (kalan TTL korunur).</summary>
    Task UpdateAsync(PreAuthEnrollmentChallenge challenge, CancellationToken cancellationToken = default);

    /// <summary>Challenge'ı siler (replay engelleme: finalize sonrası).</summary>
    Task DeleteAsync(Guid challengeToken, CancellationToken cancellationToken = default);
}
