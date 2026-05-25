using CleanTenant.Application.Common.Auth;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Application.Common.Authorization;

/// <summary>
/// Oturumu yükler ve global authorization damgası değişmişse izinleri <b>lazy</b>
/// olarak yeniden çözer (re-login gerektirmez). Request başına oturum yükleyiciler
/// (<c>SessionLookupMiddleware</c>, <c>SessionLoaderBehavior</c>) bu servisi kullanır.
/// </summary>
public interface ISessionFreshener
{
    /// <summary>
    /// Session'ı döner. Global damga, oturumun damgasından farklıysa izinler DB'den
    /// yeniden çözülür, oturum (mevcut TTL korunarak) güncellenir ve TAZE oturum döner.
    /// Bulunamazsa <c>null</c>. Support/impersonation oturumları dokunulmadan döner.
    /// </summary>
    Task<AuthSession?> GetFreshAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ISessionFreshener" />
public sealed class SessionFreshener : ISessionFreshener
{
    private readonly IAuthSessionStore _store;
    private readonly IAuthorizationStampStore _stampStore;
    private readonly IScopePermissionResolver _resolver;
    private readonly ILogger<SessionFreshener> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SessionFreshener(
        IAuthSessionStore store,
        IAuthorizationStampStore stampStore,
        IScopePermissionResolver resolver,
        ILogger<SessionFreshener> logger)
    {
        _store = store;
        _stampStore = stampStore;
        _resolver = resolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthSession?> GetFreshAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _store.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        // Support / impersonation oturumlarının izin seti özel akışla kurulur
        // (read-only elevation, true impersonation) → yeniden çözme, olduğu gibi dön.
        if (session.SupportSessionId is not null
            || !string.Equals(session.SupportMode, "None", StringComparison.Ordinal))
        {
            return session;
        }

        var currentStamp = await _stampStore.GetCurrentAsync(cancellationToken);
        if (string.Equals(session.AuthzStamp, currentStamp, StringComparison.Ordinal))
        {
            return session; // güncel — yeniden çözmeye gerek yok
        }

        // Bayat → izinleri DB'den yeniden çöz (cascade dahil) ve oturumu tazele.
        try
        {
            var (roles, permissions) = await _resolver.ResolveAsync(
                session.UserId,
                session.ScopeLevel,
                session.TenantId,
                session.CompanyId,
                session.UnitId,
                cancellationToken);

            var fresh = CloneWith(session, roles, permissions, currentStamp);
            await _store.UpdatePreservingTtlAsync(fresh, cancellationToken);
            return fresh;
        }
        catch (Exception ex)
        {
            // Best-effort: tazeleme başarısız olursa mevcut snapshot ile devam (re-login fallback).
            _logger.LogWarning(ex,
                "Oturum {SessionId} izinleri lazy tazelenemedi; mevcut snapshot kullanılıyor.", sessionId);
            return session;
        }
    }

    /// <summary>Roller/permission/damga dışındaki tüm alanları koruyarak yeni AuthSession üretir.</summary>
    private static AuthSession CloneWith(
        AuthSession s, IReadOnlyList<string> roles, IReadOnlyList<string> permissions, string stamp) => new()
    {
        SessionId = s.SessionId,
        UserId = s.UserId,
        ContextId = s.ContextId,
        Email = s.Email,
        UserName = s.UserName,
        ScopeLevel = s.ScopeLevel,
        TenantId = s.TenantId,
        TenantName = s.TenantName,
        FullName = s.FullName,
        CompanyId = s.CompanyId,
        UnitId = s.UnitId,
        Roles = roles,
        Permissions = permissions,
        PersonaSide = s.PersonaSide,
        IsSystemSession = s.IsSystemSession,
        SupportSessionId = s.SupportSessionId,
        SupportMode = s.SupportMode,
        OriginalSessionId = s.OriginalSessionId,
        ImpersonatedBy = s.ImpersonatedBy,
        IssuedAt = s.IssuedAt,
        LastActivity = s.LastActivity,
        AuthzStamp = stamp,
    };
}
