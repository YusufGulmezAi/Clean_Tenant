using System.Security.Cryptography;
using System.Text;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanTenant.Infrastructure.Identity.RefreshTokens;

/// <summary>
/// <para>
/// <see cref="IRefreshTokenService"/>'in implementasyonu. Refresh token'ları
/// kriptografik olarak güçlü rastgele üretir, SHA-256 hash'lenmiş hâlini DB'ye
/// yazar. Rotation'da replay tespiti yapar — kullanılmış token tekrar geldiyse
/// tüm zincir revoke edilir.
/// </para>
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenByteSize = 32; // 256-bit
    private readonly ICatalogDbContext _db;
    private readonly IClock _clock;
    private readonly JwtSettings _settings;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RefreshTokenService(
        ICatalogDbContext db,
        IClock clock,
        IOptions<JwtSettings> options)
    {
        _db = db;
        _clock = clock;
        _settings = options.Value;
    }

    /// <inheritdoc />
    public async Task<(string RawToken, DateTimeOffset ExpiresAt)> CreateAsync(
        Guid userId,
        Guid contextId,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        var (raw, hash) = GenerateToken();
        var now = _clock.UtcNow;
        var expiresAt = now.AddDays(_settings.RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            ContextId = contextId,
            TokenHash = hash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        });
        await _db.SaveChangesAsync(cancellationToken);

        return (raw, expiresAt);
    }

    /// <inheritdoc />
    public async Task<RefreshTokenRotationResult> RotateAsync(
        string rawToken,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        var hash = ComputeHash(rawToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null)
        {
            return Invalid("Refresh token bulunamadı.");
        }

        // Replay: zaten revoke edilmiş bir token tekrar geldi → tüm zinciri revoke et.
        if (existing.IsRevoked)
        {
            await RevokeChainInternalAsync(existing.UserId, existing.ContextId,
                "ReplayDetected", cancellationToken);
            return Invalid("Olası replay saldırısı tespit edildi; oturum zinciri revoke edildi.");
        }

        if (existing.ExpiresAt <= _clock.UtcNow)
        {
            existing.IsRevoked = true;
            existing.RevokedAt = _clock.UtcNow;
            existing.RevokedReason = "Expired";
            await _db.SaveChangesAsync(cancellationToken);
            return Invalid("Refresh token süresi dolmuş.");
        }

        // Geçerli; rotate
        var (newRaw, newHash) = GenerateToken();
        var newExpiry = _clock.UtcNow.AddDays(_settings.RefreshTokenDays);

        existing.IsRevoked = true;
        existing.RevokedAt = _clock.UtcNow;
        existing.RevokedReason = "Rotation";
        existing.ReplacedByTokenHash = newHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            ContextId = existing.ContextId,
            TokenHash = newHash,
            ExpiresAt = newExpiry,
            IsRevoked = false,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new RefreshTokenRotationResult(
            IsValid: true,
            ErrorMessage: null,
            UserId: existing.UserId,
            ContextId: existing.ContextId,
            NewRawToken: newRaw,
            NewExpiresAt: newExpiry);
    }

    /// <inheritdoc />
    public async Task RevokeChainAsync(
        Guid userId,
        Guid contextId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await RevokeChainInternalAsync(userId, contextId, reason, cancellationToken);
    }

    private async Task RevokeChainInternalAsync(
        Guid userId,
        Guid contextId,
        string reason,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.ContextId == contextId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = now;
            t.RevokedReason = reason;
        }
        if (tokens.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static (string RawToken, string Hash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteSize);
        var raw = Convert.ToBase64String(bytes);
        var hash = ComputeHash(raw);
        return (raw, hash);
    }

    private static string ComputeHash(string raw)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hashBytes);
    }

    private static RefreshTokenRotationResult Invalid(string message) =>
        new(false, message, Guid.Empty, Guid.Empty, null, DateTimeOffset.MinValue);
}
