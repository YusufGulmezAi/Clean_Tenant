using System.Security.Claims;
using System.Text;
using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Time;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CleanTenant.Infrastructure.Identity.Jwt;

/// <summary>
/// <para>
/// HMAC SHA-256 imzalı JWT üretici. JWT thin tutulur: yalnız <c>sub</c>,
/// <c>sid</c>, <c>ctx</c> + standart claim'ler (<c>iat</c>, <c>exp</c>,
/// <c>iss</c>, <c>aud</c>). Yetki claim'leri Redis session'da.
/// </para>
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials;
    private readonly JsonWebTokenHandler _handler;

    /// <summary>DI bağımlılıklarını alır; signing key'i hazırlar.</summary>
    public JwtTokenService(IOptions<JwtSettings> options, IClock clock)
    {
        _settings = options.Value;
        _clock = clock;

        var keyBytes = Encoding.UTF8.GetBytes(_settings.SigningKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key en az 32 byte (UTF-8) olmalıdır. .env'i kontrol edin.");
        }

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        _handler = new JsonWebTokenHandler();
    }

    /// <inheritdoc />
    public JwtAccessToken IssueToken(AuthSession session)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_settings.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _signingCredentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtClaimNames.Subject, session.UserId.ToString("D")),
                new Claim(JwtClaimNames.SessionId, session.SessionId.ToString("D")),
                new Claim(JwtClaimNames.ContextId, session.ContextId.ToString("D")),
            ]),
        };

        var token = _handler.CreateToken(descriptor);
        return new JwtAccessToken(token, expiresAt);
    }
}
