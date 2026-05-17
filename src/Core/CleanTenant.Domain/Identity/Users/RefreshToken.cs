using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Identity.Users;

/// <summary>
/// <para>
/// Refresh token kaydı. JWT access token süresi dolduğunda yeni access token
/// almak için sunulur. Rotation pattern uygulanır: her kullanımda yeni token
/// üretilir, eski token revoked olur ve <see cref="ReplacedByTokenHash"/>
/// alanıyla zincirlenir. Replay attack tespiti için kritik.
/// </para>
/// <para>
/// <b>Bağlam izolasyonu:</b> Her tarayıcı sekmesi (her bağlam) kendi refresh
/// token zincirine sahiptir; <see cref="ContextId"/> bu zincirin kimliğidir.
/// Bir sekmedeki refresh token'ı başka sekmede kullanılamaz.
/// </para>
/// <para>
/// <b>Saklama:</b> Token kendisi DB'de tutulmaz; sadece SHA-256 hash'i
/// (<see cref="TokenHash"/>) saklanır. Compromise durumunda token değeri
/// DB'den çıkarılamaz.
/// </para>
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    /// <summary>Token'ın bağlı olduğu kullanıcı.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tarayıcı sekmesi / persona bağlamı kimliği. Aynı kullanıcının farklı
    /// sekmelerdeki token zincirlerini birbirinden ayırır.
    /// </summary>
    public Guid ContextId { get; set; }

    /// <summary>Token değerinin SHA-256 hash'i (hex string). Asıl token saklanmaz.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Token'ın sona erme anı (UTC). Süre dolduğunda revoked olmasa bile kullanılamaz.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Token revoke edildi mi (kullanım, çıkış, compromise tespiti, vb.).</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Revocation sebebi (opsiyonel; <c>Rotation</c>, <c>UserLogout</c>, <c>Compromise</c>, <c>Expire</c>).</summary>
    public string? RevokedReason { get; set; }

    /// <summary>Revocation anı (UTC); revoke edilmediyse null.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Rotation zincirinde bu token'ın yerini alan yeni token'ın hash'i; null ise zincir burada bitti.
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>İstemci IP adresi (security audit ve aykırı kullanım tespiti).</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>İstemci tarayıcı/uygulama bilgisi (User-Agent header).</summary>
    public string UserAgent { get; set; } = string.Empty;
}
