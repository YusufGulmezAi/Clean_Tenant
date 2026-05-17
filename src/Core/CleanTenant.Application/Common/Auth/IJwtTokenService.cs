namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// JWT üretimini sağlayan servis. Verilen <see cref="AuthSession"/>'a karşılık
/// gelen ~250 byte JWT (sub, sid, ctx, iat, exp, iss, aud) üretir.
/// </para>
/// <para>
/// JWT zengin claim taşımaz; tüm yetki bilgisi Redis session'da
/// (<see cref="IAuthSessionStore"/>).
/// </para>
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Verilen session için imzalı JWT üretir.
    /// </summary>
    /// <param name="session">JWT'nin referans göstereceği session.</param>
    /// <returns>İmzalı JWT (string) ve sona erme anı.</returns>
    JwtAccessToken IssueToken(AuthSession session);
}

/// <summary>JWT üretim sonucu.</summary>
/// <param name="Token">İmzalı JWT (compact serialization).</param>
/// <param name="ExpiresAt">Sona erme anı (UTC).</param>
public sealed record JwtAccessToken(string Token, DateTimeOffset ExpiresAt);
