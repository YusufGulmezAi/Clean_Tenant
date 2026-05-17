namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// Login akışının polimorfik sonucu. <see cref="Status"/> alanı istemciye hangi
/// yan dolu geleceğini söyler:
/// <list type="bullet">
///   <item><see cref="LoginStatus.Success"/> → <see cref="Tokens"/> dolu.</item>
///   <item><see cref="LoginStatus.TwoFactorRequired"/> → <see cref="Challenge"/> dolu; istemci 2FA verify akışına geçer.</item>
/// </list>
/// </summary>
/// <param name="Status">İstemci için discriminator.</param>
/// <param name="Tokens">Başarılı login'de access + refresh + scope bilgisi.</param>
/// <param name="Challenge">2FA gerektiren login'de geçici challenge bağlamı.</param>
public sealed record LoginResult(
    LoginStatus Status,
    TokenPair? Tokens,
    TwoFactorChallengeResponse? Challenge);

/// <summary>Login akışı sonuç tipi.</summary>
public enum LoginStatus
{
    /// <summary>Şifre + (varsa) 2FA doğru; <c>TokenPair</c> döner.</summary>
    Success = 1,

    /// <summary>Şifre doğru ama 2FA gerekli; istemci challenge token'la <c>/auth/2fa/verify</c>'ye gider.</summary>
    TwoFactorRequired = 2,
}

/// <summary>
/// İstemciye dönen 2FA challenge bilgisi. Hassas alanlar (UserId, IpAddress vb.)
/// dahil edilmez — istemci sadece bu yeterli.
/// </summary>
/// <param name="ChallengeToken">5 dk TTL'li geçici doğrulama token'ı (Redis'te).</param>
/// <param name="ExpiresAt">Token sona erme anı (UTC).</param>
/// <param name="AvailableMethods">Kullanıcının kullanabileceği 2FA yöntemleri (<c>"Authenticator"</c>, <c>"Email"</c>, <c>"Phone"</c>); recovery code ayrıca her zaman geçerli.</param>
public sealed record TwoFactorChallengeResponse(
    Guid ChallengeToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<string> AvailableMethods);
