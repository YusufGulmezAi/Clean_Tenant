namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// Kullanıcının <c>RequiresPasswordChange = true</c> bayrağıyla giriş yaptığında
/// üretilen geçici bağlam. Kullanıcı henüz authenticated değil — yalnız parolası
/// doğrulandı; cookie/JWT verilmemiş. Token Redis'te 15 dk tutulur; bu pencerede
/// /change-password akışı tamamlanmalı.
/// Akış başarıyla tamamlandığında challenge silinir, RequiresPasswordChange = false
/// set edilir, ardından normal LoginFinalizer ile TokenPair üretilir.
/// </summary>
public sealed class PasswordChangeChallenge
{
    /// <summary>Geçici challenge id'si (Guid V7); istemciye opaque token olarak verilir.</summary>
    public Guid ChallengeToken { get; init; }

    /// <summary>Şifresini değiştirecek kullanıcı.</summary>
    public Guid UserId { get; init; }

    /// <summary>Kullanıcı e-postası (UX için — sayfada gösterimde).</summary>
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
}
