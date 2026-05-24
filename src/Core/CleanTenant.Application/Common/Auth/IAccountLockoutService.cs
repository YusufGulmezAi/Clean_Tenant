using CleanTenant.Domain.Identity.Users;

namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Tenant-başına hesap kilitleme mantığını merkezi tutar. Hem şifre login
/// (<c>LoginCommandHandler</c>) hem 2FA doğrulama (<c>VerifyTwoFactorCommandHandler</c>)
/// hatalı denemede bu servisi kullanır — böylece kilit eşiği/süresi tek yerden
/// ve tenant ayarına göre uygulanır.
/// </para>
/// <para>
/// <b>Neden merkezi:</b> ASP.NET Identity'nin yerleşik otomatik kilidi global
/// (tek eşik) olduğu için devre dışı bırakıldı (bkz. IdentityOptions
/// <c>MaxFailedAccessAttempts</c> yüksek sentinel). Kilit kararı artık burada,
/// çözümlenen <see cref="LockoutPolicy"/>'ye göre verilir.
/// </para>
/// </summary>
public interface IAccountLockoutService
{
    /// <summary>
    /// Kullanıcı için geçerli kilitleme politikasını çözer. Kullanıcı tam olarak
    /// bir tenant'a bağlıysa o tenant'ın ayarı; aksi halde (System / çok-tenant)
    /// <see cref="LockoutPolicy.Default"/>.
    /// </summary>
    Task<LockoutPolicy> ResolvePolicyAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>
    /// Hatalı bir kimlik doğrulama denemesini kaydeder: sayacı artırır ve
    /// politikadaki eşiğe ulaşıldıysa hesabı kilitler (sayacı sıfırlayıp kilit
    /// bitiş zamanını ayarlar).
    /// </para>
    /// <para>
    /// Hesap bu çağrıyla kilitlendiyse kilit bitiş zamanını döner; aksi halde
    /// (sayaç eşiğin altında veya kilitleme kapalı) <c>null</c>.
    /// </para>
    /// </summary>
    Task<DateTimeOffset?> RegisterFailedAttemptAsync(User user, CancellationToken cancellationToken = default);
}
