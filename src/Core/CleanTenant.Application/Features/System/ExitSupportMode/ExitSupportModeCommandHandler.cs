using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanTenant.Application.Features.System.ExitSupportMode;

/// <summary>
/// Support Mode'dan çıkış işlemi:
/// <list type="number">
///   <item>Mevcut session Support Mode olmalı (policy doğrular).</item>
///   <item><c>SupportSession.EndedAt</c> set edilir.</item>
///   <item>Mevcut Redis support session silinir.</item>
///   <item>OriginalSessionId hâlâ Redis'te ise yeni JWT issue edilir.</item>
///   <item>Yoksa "orijinal session sona ermiş" hatası.</item>
/// </list>
/// </summary>
public sealed class ExitSupportModeCommandHandler
{
    private readonly ICatalogDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthSessionStore _sessionStore;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IClock _clock;
    private readonly JwtSettings _jwtSettings;
    private readonly SessionSettings _sessionSettings;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ExitSupportModeCommandHandler(
        ICatalogDbContext db,
        IJwtTokenService jwtTokenService,
        IAuthSessionStore sessionStore,
        ICurrentSessionAccessor sessionAccessor,
        IClock clock,
        IOptions<JwtSettings> jwtOptions,
        IOptions<SessionSettings> sessionOptions)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _sessionStore = sessionStore;
        _sessionAccessor = sessionAccessor;
        _clock = clock;
        _jwtSettings = jwtOptions.Value;
        _sessionSettings = sessionOptions.Value;
    }

    /// <summary>Exit Support Mode isteğini işler.</summary>
    public async Task<Result<ExitSupportModeResult>> HandleAsync(
        ExitSupportModeCommand command,
        CancellationToken cancellationToken)
    {
        var current = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        if (current.SupportSessionId is null || current.OriginalSessionId is null)
        {
            return Result<ExitSupportModeResult>.Failure(
                Error.Failure("SUP-003", "Aktif Support Mode oturumu bulunamadı."));
        }

        var now = _clock.UtcNow;

        // SupportSession DB kaydını sonlandır
        var supportSession = await _db.SupportSessions
            .FirstOrDefaultAsync(s => s.Id == current.SupportSessionId.Value, cancellationToken);
        if (supportSession is not null && supportSession.EndedAt is null)
        {
            supportSession.EndedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Orijinal session hâlâ aktif mi
        var original = await _sessionStore.GetAsync(current.OriginalSessionId.Value, cancellationToken);

        // Mevcut support session'ı sil
        await _sessionStore.DeleteAsync(current.SessionId, current.UserId, cancellationToken);

        if (original is null)
        {
            return Result<ExitSupportModeResult>.Failure(
                Error.Unauthorized("SUP-004",
                    "Orijinal oturum sona ermiş. Lütfen tekrar login olun."));
        }

        // TTL yenile (sliding) ve yeni JWT
        var originalTtl = TimeSpan.FromMinutes(
            _jwtSettings.AccessTokenMinutes + _sessionSettings.TtlPaddingMinutes);
        original.LastActivity = now;
        await _sessionStore.UpdateAsync(original, originalTtl, cancellationToken);

        var jwt = _jwtTokenService.IssueToken(original);

        return Result<ExitSupportModeResult>.Success(new ExitSupportModeResult(
            jwt.Token,
            jwt.ExpiresAt,
            original.SessionId,
            original.ContextId));
    }
}

/// <summary>Support Mode çıkış sonucu — yalnız yeni access token (refresh aynı kalır).</summary>
/// <param name="AccessToken">Orijinal session için yeni JWT.</param>
/// <param name="AccessTokenExpiresAt">Sona erme anı (UTC).</param>
/// <param name="SessionId">Orijinal (geri dönülen) session id.</param>
/// <param name="ContextId">Sekme context id.</param>
public sealed record ExitSupportModeResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    Guid SessionId,
    Guid ContextId);
