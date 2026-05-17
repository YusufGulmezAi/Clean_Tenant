using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Auth.LogoutAllSessions;

/// <summary>
/// Kullanıcının tüm aktif Redis session'larını siler ve tüm refresh token
/// zincirlerini (tüm context'ler için) revoke eder.
/// </summary>
public sealed class LogoutAllSessionsCommandHandler
{
    private readonly IAuthSessionStore _sessionStore;
    private readonly ICatalogDbContext _db;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LogoutAllSessionsCommandHandler(
        IAuthSessionStore sessionStore,
        ICatalogDbContext db,
        ICurrentSessionAccessor sessionAccessor)
    {
        _sessionStore = sessionStore;
        _db = db;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Logout-all işlemini uygular.</summary>
    public async Task<Result> HandleAsync(LogoutAllSessionsCommand command, CancellationToken cancellationToken)
    {
        var current = _sessionAccessor.Current;
        if (current is null)
        {
            return Result.Failure(Error.Unauthorized("AUTH-008", "Mevcut oturum bulunamadı."));
        }

        // Tüm aktif Redis session'lar
        await _sessionStore.DeleteAllForUserAsync(current.UserId, cancellationToken);

        // Tüm aktif refresh token'ları revoke et (her context için)
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == current.UserId && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var t in activeTokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = now;
            t.RevokedReason = "UserLogoutAll";
        }
        if (activeTokens.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
