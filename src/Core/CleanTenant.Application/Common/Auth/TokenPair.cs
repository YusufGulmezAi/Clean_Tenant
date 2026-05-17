namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Login, refresh ve switch-context işlemlerinin sonucu olarak istemciye dönen
/// token çifti. Access token kısa ömürlü (15-30 dk); refresh token uzun ömürlü
/// (7 gün) ve rotation chain ile yönetilir.
/// </para>
/// <para>
/// <see cref="CurrentScope"/> aktif scope'tur; <see cref="AvailableScopes"/>
/// kullanıcının erişebileceği tüm scope'lardır (persona ile filtrelenmiş).
/// İstemci tek scope varsa direkt kullanır; çoklu scope varsa scope seçici sunar.
/// </para>
/// </summary>
/// <param name="AccessToken">Bearer kullanılan JWT (yalın: sub, sid, ctx).</param>
/// <param name="AccessTokenExpiresAt">Access token sona erme anı (UTC).</param>
/// <param name="RefreshToken">Refresh için kullanılacak uzun ömürlü token (raw; bir kere döner).</param>
/// <param name="RefreshTokenExpiresAt">Refresh token sona erme anı (UTC).</param>
/// <param name="SessionId">Redis session kimliği; istemci debug/log için takip eder.</param>
/// <param name="ContextId">Sekme/persona context kimliği; istemci sessionStorage'da tutar.</param>
/// <param name="CurrentScope">Aktif scope (JWT'nin bağlı olduğu).</param>
/// <param name="AvailableScopes">Kullanıcının erişebileceği tüm scope'lar (persona ile filtrelenmiş).</param>
public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid SessionId,
    Guid ContextId,
    ScopeOption CurrentScope,
    IReadOnlyList<ScopeOption> AvailableScopes);
