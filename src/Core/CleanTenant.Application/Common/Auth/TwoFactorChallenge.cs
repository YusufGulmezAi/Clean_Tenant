namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Login akışı sırasında 2FA gerektiren bir kullanıcı için sunucunun ürettiği
/// geçici doğrulama bağlamı. İstemci bu nesneden yalnız <see cref="ChallengeToken"/>'ı
/// görür; diğer alanlar sunucu tarafında Redis'te saklanır.
/// </para>
/// <para>
/// TTL kısa (5 dk). İstemci süreyi aşarsa baştan login olur. Token bir kez
/// doğrulanır ve silinir (replay engellenir).
/// </para>
/// </summary>
public sealed class TwoFactorChallenge
{
    /// <summary>Geçici challenge id'si (Guid V7); istemciye opaque token olarak verilir.</summary>
    public Guid ChallengeToken { get; init; }

    /// <summary>Doğrulanmakta olan kullanıcı.</summary>
    public Guid UserId { get; init; }

    /// <summary>Login akışında verilen sekme/persona kimliği — login tamamlanınca aynı id session'da kullanılır.</summary>
    public Guid ContextId { get; init; }

    /// <summary>Login akışında seçilen persona — finalize'da TokenPair için aynısı kullanılır.</summary>
    public PersonaSide Persona { get; init; }

    /// <summary>İstemci IP (audit).</summary>
    public string IpAddress { get; init; } = string.Empty;

    /// <summary>İstemci User-Agent (audit).</summary>
    public string UserAgent { get; init; } = string.Empty;

    /// <summary>Challenge oluşturulma anı.</summary>
    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// Kullanıcının bu doğrulamada seçebileceği yöntemler. Identity'nin
    /// <c>UserManager.GetValidTwoFactorProvidersAsync</c> sonucu — örn.
    /// <c>["Authenticator", "Email", "Phone"]</c>. <c>RecoveryCode</c> ayrı
    /// (her kullanıcıda var; ayrıca listelemiyoruz).
    /// </summary>
    public IReadOnlyList<string> AvailableMethods { get; init; } = [];
}
