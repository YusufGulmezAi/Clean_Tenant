using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.ForceLogoutUser;

/// <summary>
/// <para>
/// Admin tarafından bir kullanıcının tüm session'larını sonlandırır. Her ne
/// kadar endpoint seviyesinde policy ile kim çağırabilir kısıtlanmış olsa da
/// handler ek doğrulama yapmaz — niyetli bir çağrıdır.
/// </para>
/// <para>
/// <c>Reason</c> alanı min 20 karakter; audit log'a düşer (v0.1.7'de
/// AuditInterceptor ile dolacak; v0.1.5.b.1'de yalnızca komut zorunluluğu).
/// </para>
/// </summary>
public sealed class ForceLogoutUserCommandHandler
{
    private readonly ICatalogDbContext _db;
    private readonly IAuthSessionStore _sessionStore;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ForceLogoutUserCommandHandler(
        ICatalogDbContext db,
        IAuthSessionStore sessionStore)
    {
        _db = db;
        _sessionStore = sessionStore;
    }

    /// <summary>Force-logout uygular.</summary>
    public async Task<Result> HandleAsync(ForceLogoutUserCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Length < 20)
        {
            return Result.Failure(
                Error.Validation("AUTH-012", "Sebep zorunlu (minimum 20 karakter)."));
        }

        var target = await _db.Users
            .AsNoTracking()
            .Where(u => u.UrlCode == command.TargetUserUrlCode)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync(cancellationToken);
        if (target is null)
        {
            return Result.Failure(
                Error.NotFound("AUTH-013", "Hedef kullanıcı bulunamadı."));
        }

        // Redis session'lar
        await _sessionStore.DeleteAllForUserAsync(target.Id, cancellationToken);

        // Refresh token'lar
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == target.Id && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var t in activeTokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = now;
            t.RevokedReason = "AdminForceLogout";
        }
        if (activeTokens.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
