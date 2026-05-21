using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// <see cref="DeactivateUserCommand"/> handler. Soft-delete + session iptal.
/// Kendi hesabını devre dışı bırakma engellenir.
/// </summary>
public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly ICatalogDbContext _db;
    private readonly IAuthSessionStore _sessionStore;
    private readonly ICurrentSessionAccessor _session;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeactivateUserCommandHandler(
        ICatalogDbContext db,
        IAuthSessionStore sessionStore,
        ICurrentSessionAccessor session)
    {
        _db = db;
        _sessionStore = sessionStore;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(u => u.UrlCode == command.UrlCode && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("USER-001", "Kullanıcı bulunamadı."));

        // Kendi hesabını devre dışı bırakma engeli
        var currentUserId = _session.Current?.UserId;
        if (currentUserId == user.Id)
            return Result.Failure(Error.Forbidden("USER-007", "Kendi hesabınızı devre dışı bırakamazsınız."));

        if (!user.IsActive)
            return Result.Failure(Error.Conflict("USER-008", "Kullanıcı zaten devre dışı."));

        var now = DateTimeOffset.UtcNow;
        user.IsActive = false;
        user.UpdatedAt = now;
        user.UpdatedBy = currentUserId;

        // Aktif session'ları sonlandır
        await _sessionStore.DeleteAllForUserAsync(user.Id, cancellationToken);

        // Refresh token'ları iptal et
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
            token.RevokedReason = "UserDeactivated";
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
