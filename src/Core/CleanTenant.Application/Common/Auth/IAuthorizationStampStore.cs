namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Global "authorization damgası" deposu. Damga, sistemdeki yetki tanımlarının
/// (rol izinleri, rol-kullanıcı atamaları, rol silme/yeniden adlandırma) güncel
/// sürümünü temsil eden opak bir değerdir.
/// </para>
/// <para>
/// <b>Lazy izin tazeleme:</b> Her oturum, izinlerini çözdüğü andaki damgayı saklar
/// (<see cref="AuthSession.AuthzStamp"/>). Herhangi bir yetki değişiminde
/// <see cref="BumpAsync"/> çağrılır; bir sonraki istekte oturum damgası global
/// damgayla eşleşmiyorsa izinler yeniden çözülür — kullanıcı yeniden giriş yapmaz.
/// </para>
/// </summary>
public interface IAuthorizationStampStore
{
    /// <summary>Güncel global damgayı döner. Hiç set edilmemişse stabil bir varsayılan (<c>"0"</c>).</summary>
    Task<string> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Damgayı yeni bir değere yükseltir — tüm aktif oturumları "bayat" yapar.</summary>
    Task BumpAsync(CancellationToken cancellationToken = default);
}
