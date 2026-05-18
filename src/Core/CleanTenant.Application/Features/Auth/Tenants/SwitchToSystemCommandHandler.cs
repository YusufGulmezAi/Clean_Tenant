using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanTenant.Application.Features.Auth.Tenants;

/// <summary>
/// <see cref="SwitchToSystemCommand"/> handler'ı. Kullanıcının System scope
/// rol ataması varsa Tenant scope'tan System'e geri döner. Tenant'a "yanlışlıkla"
/// geçen System operatörler için geri çıkış yolu.
/// </summary>
public sealed class SwitchToSystemCommandHandler : IRequestHandler<SwitchToSystemCommand, Result<TokenPair>>
{
    private readonly ICatalogDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthSessionStore _sessionStore;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IClock _clock;
    private readonly JwtSettings _jwtSettings;
    private readonly SessionSettings _sessionSettings;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SwitchToSystemCommandHandler(
        ICatalogDbContext db,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IAuthSessionStore sessionStore,
        ICurrentSessionAccessor sessionAccessor,
        IClock clock,
        IOptions<JwtSettings> jwtOptions,
        IOptions<SessionSettings> sessionOptions)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _sessionStore = sessionStore;
        _sessionAccessor = sessionAccessor;
        _clock = clock;
        _jwtSettings = jwtOptions.Value;
        _sessionSettings = sessionOptions.Value;
    }

    /// <summary>Switch-to-system isteğini işler.</summary>
    public async Task<Result<TokenPair>> Handle(SwitchToSystemCommand command, CancellationToken cancellationToken)
    {
        var current = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null. Endpoint Bearer/Cookie korumalı olmalı.");

        // Persona uyumu — Management dışı System scope'a giremez.
        if (current.PersonaSide != PersonaSide.Management)
        {
            return Result<TokenPair>.Failure(
                Error.Forbidden("AUTH-010",
                    $"Persona '{current.PersonaSide}' System scope'a geçemez."));
        }

        // System scope rol ataması var mı?
        var systemAssignments = await _db.UserRoleAssignments.AsNoTracking()
            .Where(a => a.UserId == current.UserId
                     && a.IsActive
                     && a.ScopeLevel == ScopeLevel.System)
            .ToListAsync(cancellationToken);

        if (systemAssignments.Count == 0)
        {
            return Result<TokenPair>.Failure(
                Error.Forbidden("AUTH-013",
                    "System scope için aktif rol ataması yok."));
        }

        // System scope'taki tüm rolleri + permission'ları yükle (tek scope = System).
        var roles = await _db.UserRoleAssignments.AsNoTracking()
            .Where(a => a.UserId == current.UserId
                     && a.IsActive
                     && a.ScopeLevel == ScopeLevel.System)
            .Join(_db.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name!)
            .ToListAsync(cancellationToken);

        var permissions = await _db.UserRoleAssignments.AsNoTracking()
            .Where(a => a.UserId == current.UserId
                     && a.IsActive
                     && a.ScopeLevel == ScopeLevel.System)
            .Join(_db.RolePermissions, a => a.RoleId, rp => rp.RoleId, (a, rp) => rp.PermissionId)
            .Join(_db.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        var newSessionId = Guid.CreateVersion7(now);

        // v0.2.3.c — Support Mode v2: Eğer mevcut session bir SupportSession
        // taşıyorsa (Sistem operatörü destek bağlamından çıkıyor), o oturumu
        // EndedAt ile kapat. UI banner kalkar; tenant admin "Destek Erişim
        // Geçmişi" sayfasında bu kaydı kapalı görür.
        if (current.SupportSessionId is { } supportSessionId)
        {
            var openSession = await _db.SupportSessions
                .FirstOrDefaultAsync(s => s.Id == supportSessionId && s.EndedAt == null, cancellationToken);
            if (openSession is not null)
            {
                openSession.EndedAt = now;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        var newSession = new AuthSession
        {
            SessionId = newSessionId,
            UserId = current.UserId,
            ContextId = current.ContextId,
            Email = current.Email,
            UserName = current.UserName,
            FullName = current.FullName,
            ScopeLevel = ScopeLevel.System,
            TenantId = null,
            TenantName = null,
            CompanyId = null,
            UnitId = null,
            Roles = roles,
            Permissions = permissions,
            PersonaSide = current.PersonaSide,
            IsSystemSession = current.IsSystemSession,
            IssuedAt = now,
            LastActivity = now,
        };

        var sessionTtl = TimeSpan.FromMinutes(_jwtSettings.AccessTokenMinutes + _sessionSettings.TtlPaddingMinutes);
        await _sessionStore.StoreAsync(newSession, sessionTtl, cancellationToken);
        await _sessionStore.DeleteAsync(current.SessionId, current.UserId, cancellationToken);

        await _refreshTokenService.RevokeChainAsync(
            current.UserId, current.ContextId, "RevertToSystem", cancellationToken);
        var (rawRefresh, refreshExpiry) = await _refreshTokenService.CreateAsync(
            current.UserId, current.ContextId, command.IpAddress, command.UserAgent, cancellationToken);

        var jwt = _jwtTokenService.IssueToken(newSession);

        var currentScope = new ScopeOption(
            newSession.ScopeLevel,
            newSession.TenantId,
            newSession.CompanyId,
            newSession.UnitId);

        return Result<TokenPair>.Success(new TokenPair(
            jwt.Token,
            jwt.ExpiresAt,
            rawRefresh,
            refreshExpiry,
            newSessionId,
            current.ContextId,
            currentScope,
            AvailableScopes: []));
    }
}
