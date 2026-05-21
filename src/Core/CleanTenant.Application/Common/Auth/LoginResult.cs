namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// Login akışının polimorfik sonucu. <see cref="Status"/> alanı istemciye hangi
/// yan dolu geleceğini söyler:
/// <list type="bullet">
///   <item><see cref="LoginStatus.Success"/> → <see cref="Tokens"/> dolu.</item>
///   <item><see cref="LoginStatus.TwoFactorRequired"/> → <see cref="Challenge"/> dolu; istemci 2FA verify akışına geçer.</item>
///   <item><see cref="LoginStatus.EnrollmentRequired"/> → <see cref="EnrollmentChallenge"/> dolu; istemci pre-auth enrollment akışına geçer (System scope kullanıcısı + 2FA yok).</item>
/// </list>
/// </summary>
/// <param name="Status">İstemci için discriminator.</param>
/// <param name="Tokens">Başarılı login'de access + refresh + scope bilgisi.</param>
/// <param name="Challenge">2FA gerektiren login'de geçici challenge bağlamı.</param>
/// <param name="EnrollmentChallenge">System scope kullanıcısı için pre-auth 2FA enrollment challenge'ı.</param>
/// <param name="PasswordChangeChallenge">İlk giriş şifre değişimi gerektiren kullanıcı için geçici challenge.</param>
public sealed record LoginResult(
    LoginStatus Status,
    TokenPair? Tokens,
    TwoFactorChallengeResponse? Challenge,
    PreAuthEnrollmentChallengeResponse? EnrollmentChallenge = null,
    PasswordChangeChallengeResponse? PasswordChangeChallenge = null);

/// <summary>Login akışı sonuç tipi.</summary>
public enum LoginStatus
{
    /// <summary>Şifre + (varsa) 2FA doğru; <c>TokenPair</c> döner.</summary>
    Success = 1,

    /// <summary>Şifre doğru ama 2FA gerekli; istemci challenge token'la <c>/auth/2fa/verify</c>'ye gider.</summary>
    TwoFactorRequired = 2,

    /// <summary>
    /// Şifre doğru, kullanıcı System scope rolünde ama 2FA aktif değil — pre-auth
    /// enrollment akışı gerekli. İstemci enrollment token'la
    /// <c>/2fa/enroll-pre-auth</c> sayfasına yönlendirilir. v0.2.2.a'da eklendi.
    /// </summary>
    EnrollmentRequired = 3,

    /// <summary>
    /// Şifre doğru ama kullanıcının <c>RequiresPasswordChange = true</c> bayrağı var —
    /// ilk girişte admin tarafından atanmış geçici şifreyi değiştirmesi zorunlu.
    /// İstemci <c>/change-password</c> sayfasına yönlendirilir.
    /// </summary>
    PasswordChangeRequired = 4,
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

/// <summary>
/// İstemciye dönen pre-auth 2FA enrollment challenge bilgisi. Kullanıcı System
/// scope rolünde + 2FA aktif değil. İstemci bu token ile <c>/2fa/enroll-pre-auth</c>
/// sayfasına yönlendirilir; QR + kod doğrulama + recovery code adımlarından
/// sonra finalize ile cookie kurulur.
/// </summary>
/// <param name="ChallengeToken">10 dk TTL'li geçici enrollment token (Redis'te).</param>
/// <param name="ExpiresAt">Token sona erme anı (UTC).</param>
/// <param name="Email">Kullanıcı e-postası (sayfada gösterim için, kimlik bilgisi değil).</param>
public sealed record PreAuthEnrollmentChallengeResponse(
    Guid ChallengeToken,
    DateTimeOffset ExpiresAt,
    string Email);

/// <summary>
/// İstemciye dönen şifre değişim challenge bilgisi. Kullanıcının
/// <c>RequiresPasswordChange = true</c> bayrağı var; ilk girişte
/// geçici şifresini değiştirmeli. İstemci bu token ile
/// <c>/change-password</c> sayfasına yönlendirilir.
/// </summary>
/// <param name="ChallengeToken">15 dk TTL'li geçici token (Redis'te).</param>
/// <param name="ExpiresAt">Token sona erme anı (UTC).</param>
/// <param name="Email">Kullanıcı e-postası (sayfada gösterim için).</param>
public sealed record PasswordChangeChallengeResponse(
    Guid ChallengeToken,
    DateTimeOffset ExpiresAt,
    string Email);
