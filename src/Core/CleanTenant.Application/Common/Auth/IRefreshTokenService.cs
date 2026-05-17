namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Refresh token üretimi, rotation chain yönetimi, replay tespiti ve
/// revocation servisi. Refresh token'lar DB'de hash'lenmiş olarak tutulur
/// (raw token sadece kullanıcıya bir kere döner).
/// </para>
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Yeni bir refresh token üretir ve DB'ye kaydeder.
    /// </summary>
    /// <param name="userId">Token sahibi kullanıcı.</param>
    /// <param name="contextId">Token zincirinin bağlı olduğu context.</param>
    /// <param name="ipAddress">İstemci IP'si (denetim için).</param>
    /// <param name="userAgent">İstemci tarayıcı bilgisi (denetim için).</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>Raw refresh token (yalnızca burada üretilir) + sona erme anı.</returns>
    Task<(string RawToken, DateTimeOffset ExpiresAt)> CreateAsync(
        Guid userId,
        Guid contextId,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen raw refresh token'ı doğrular ve rotation uygular.
    /// Replay tespit edilirse zincir revoke edilir, hata döner.
    /// </summary>
    /// <param name="rawToken">İstemcinin sunduğu raw refresh token.</param>
    /// <param name="ipAddress">İstemci IP'si.</param>
    /// <param name="userAgent">İstemci tarayıcı bilgisi.</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>Doğrulama başarılıysa (UserId, ContextId, yeni raw token, yeni expiry).</returns>
    Task<RefreshTokenRotationResult> RotateAsync(
        string rawToken,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen context'in tüm refresh token zincirini revoke eder (logout).
    /// </summary>
    Task RevokeChainAsync(
        Guid userId,
        Guid contextId,
        string reason,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Refresh rotation sonucu.
/// </summary>
/// <param name="IsValid">Rotation başarılı mı.</param>
/// <param name="ErrorMessage">Başarısızsa açıklama.</param>
/// <param name="UserId">Başarılıysa kullanıcı id'si.</param>
/// <param name="ContextId">Başarılıysa context id'si.</param>
/// <param name="NewRawToken">Başarılıysa yeni raw token.</param>
/// <param name="NewExpiresAt">Başarılıysa yeni sona erme anı.</param>
public sealed record RefreshTokenRotationResult(
    bool IsValid,
    string? ErrorMessage,
    Guid UserId,
    Guid ContextId,
    string? NewRawToken,
    DateTimeOffset NewExpiresAt);
