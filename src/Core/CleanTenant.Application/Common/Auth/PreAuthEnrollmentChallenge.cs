namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Login akışı System scope rolündeki kullanıcı için 2FA enrollment zorunlu
/// görünce ürettiği geçici bağlam. Kullanıcı henüz authenticated <em>değil</em> —
/// yalnız parolası doğrulandı; cookie/JWT verilmemiş. Token Redis'te 10dk
/// tutulur, enrollment akışı (QR tara → kod doğrula → recovery codes) bu
/// pencerede tamamlanmalı.
/// </para>
/// <para>
/// Akış başarıyla finalize edildiğinde challenge silinir + normal
/// <c>LoginFinalizer</c>'la <c>TokenPair</c> üretilir (Persona ve
/// ContextId burada saklı). Süre dolarsa veya kullanıcı iptal ederse baştan
/// login olur.
/// </para>
/// </summary>
public sealed class PreAuthEnrollmentChallenge
{
    /// <summary>Geçici challenge id'si (Guid V7); istemciye opaque token olarak verilir.</summary>
    public Guid ChallengeToken { get; init; }

    /// <summary>Enrollment yapacak kullanıcı.</summary>
    public Guid UserId { get; init; }

    /// <summary>Kullanıcı e-postası (UX için — sayfada "X için 2FA enrollment" gibi gösterimde).</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Login akışında verilen sekme/persona kimliği — finalize'da aynısı kullanılır.</summary>
    public Guid ContextId { get; init; }

    /// <summary>Login akışında seçilen persona — finalize'da TokenPair için aynısı kullanılır.</summary>
    public PersonaSide Persona { get; init; }

    /// <summary>İstemci IP (audit + finalize).</summary>
    public string IpAddress { get; init; } = string.Empty;

    /// <summary>İstemci User-Agent (audit + finalize).</summary>
    public string UserAgent { get; init; } = string.Empty;

    /// <summary>Challenge oluşturulma anı.</summary>
    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// Kullanıcı TOTP kodunu doğrulayıp 2FA aktif ettiğinde set edilir. Null'sa
    /// henüz doğrulama yapılmadı — finalize endpoint'i null challenge'ı reddeder
    /// (replay/atlatma engeli).
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; set; }
}
