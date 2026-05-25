namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Redis-backed auth session store sözleşmesi. Login'de session yazılır,
/// her HTTP isteğinde okunur (SessionLookupMiddleware), logout'ta silinir.
/// </para>
/// <para>
/// <b>Anlık revocation:</b> Yetki değişiminde / kullanıcı kilitlenmesinde
/// store doğrudan güncellenir veya silinir; token expiry beklenmez.
/// </para>
/// </summary>
public interface IAuthSessionStore
{
    /// <summary>
    /// Yeni session'ı Redis'e yazar; TTL set eder; <c>user:{userId}:sessions</c>
    /// set'ine ekler (toplu revocation için).
    /// </summary>
    Task StoreAsync(AuthSession session, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen session'ı Redis'ten okur. Yoksa null (revoked / TTL doldu).
    /// </summary>
    Task<AuthSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>LastActivity</c>'i günceller ve sliding TTL ile süreyi uzatır.
    /// </summary>
    Task TouchAsync(Guid sessionId, TimeSpan newTtl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mevcut session'ın tüm içeriğini değiştirir (in-place update).
    /// Elevate-to-write akışı gibi mutation'lar için kullanılır.
    /// <c>LastActivity</c> caller tarafından güncellenebilir.
    /// </summary>
    Task UpdateAsync(AuthSession session, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Session'ı Redis'ten siler (logout). User index'inden çıkarır.
    /// </summary>
    Task DeleteAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir kullanıcının tüm aktif session'larını siler (compromise / admin force-logout).
    /// </summary>
    Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir kullanıcının tüm aktif session id'lerini döner (toplu güncelleme için).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveSessionIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Session içeriğini, Redis'teki <b>mevcut TTL'i koruyarak</b> günceller (sliding
    /// pencereyi sıfırlamaz). Rol izni değişiminde aktif oturumların izin snapshot'ını
    /// re-login gerektirmeden tazelemek için kullanılır.
    /// </summary>
    /// <returns>Session mevcuttu ve güncellendiyse <c>true</c>; bulunamadıysa <c>false</c>.</returns>
    Task<bool> UpdatePreservingTtlAsync(AuthSession session, CancellationToken cancellationToken = default);
}
