using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using MediatR;

namespace CleanTenant.Application.Features.Auth.SwitchContext;

/// <summary>
/// <para>
/// Switch-context akışını yürüten handler. Adımlar:
/// </para>
/// <list type="number">
///   <item>Mevcut session'dan UserId + Persona oku.</item>
///   <item>Hedef scope kullanıcının aktif atamasında mı kontrol et.</item>
///   <item>Persona uyumu doğrula (Management → System/Tenant/Company, Portal → Unit).</item>
///   <item>Hedef scope için roller + permission'lar yükle.</item>
///   <item>Yeni sessionId üret; aynı contextId ile yeni Redis session yaz.</item>
///   <item>Eski Redis session'ı sil; refresh token zincirini revoke et + yeni token yarat.</item>
///   <item>Yeni JWT döner.</item>
/// </list>
/// </summary>
public sealed class SwitchContextCommandHandler : IRequestHandler<SwitchContextCommand, Result<TokenPair>>
{
    private readonly ICatalogDbContext _db;
    private readonly IScopePermissionResolver _resolver;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthSessionStore _sessionStore;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IClock _clock;
    private readonly JwtSettings _jwtSettings;
    private readonly SessionSettings _sessionSettings;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SwitchContextCommandHandler(
        ICatalogDbContext db,
        IScopePermissionResolver resolver,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IAuthSessionStore sessionStore,
        ICurrentSessionAccessor sessionAccessor,
        IClock clock,
        IOptions<JwtSettings> jwtOptions,
        IOptions<SessionSettings> sessionOptions)
    {
        _db = db;
        _resolver = resolver;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _sessionStore = sessionStore;
        _sessionAccessor = sessionAccessor;
        _clock = clock;
        _jwtSettings = jwtOptions.Value;
        _sessionSettings = sessionOptions.Value;
    }

    /// <summary>Switch-context isteğini işler.</summary>
    public async Task<Result<TokenPair>> Handle(SwitchContextCommand command, CancellationToken cancellationToken)
    {
        var current = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null. Endpoint Bearer korumalı olmalı.");

        // Persona uyumu kontrolü
        var allowedByPersona = current.PersonaSide switch
        {
            PersonaSide.Management => command.TargetLevel is ScopeLevel.System or ScopeLevel.Tenant or ScopeLevel.Company,
            PersonaSide.Portal => command.TargetLevel is ScopeLevel.Unit,
            _ => false,
        };
        if (!allowedByPersona)
        {
            return Result<TokenPair>.Failure(
                Error.Forbidden("AUTH-010",
                    $"Persona '{current.PersonaSide}' bu scope seviyesine ({command.TargetLevel}) geçemez."));
        }

        // Hedef scope kullanıcının atamasında var mı? Cascade (v0.2.13.e): Company
        // hedefinde, parent tenant'ta Tenant-scope ataması olan kullanıcı (TenantAdmin)
        // da kabul edilir — aksi halde resolver hiç çalışmadan AUTH-011'de takılırdı.
        var hasAssignment = await _db.UserRoleAssignments
            .AsNoTracking()
            .AnyAsync(a => a.UserId == current.UserId
                        && a.IsActive
                        && (
                               (a.ScopeLevel == command.TargetLevel
                                && a.TenantId == command.TargetTenantId
                                && a.CompanyId == command.TargetCompanyId
                                && a.UnitId == command.TargetUnitId)
                               || (command.TargetLevel == ScopeLevel.Company
                                   && a.ScopeLevel == ScopeLevel.Tenant
                                   && a.TenantId == command.TargetTenantId
                                   && a.CompanyId == null
                                   && a.UnitId == null)
                           ),
                cancellationToken);
        if (!hasAssignment)
        {
            return Result<TokenPair>.Failure(
                Error.Forbidden("AUTH-011", "Hedef scope için aktif atama yok."));
        }

        // Yeni scope'taki roller + permission'lar — cascade kuralını içeren ortak
        // resolver üzerinden çözülür.
        var (roles, permissions) = await _resolver.ResolveAsync(
            current.UserId,
            command.TargetLevel,
            command.TargetTenantId,
            command.TargetCompanyId,
            command.TargetUnitId,
            cancellationToken);

        var now = _clock.UtcNow;
        var newSessionId = Guid.CreateVersion7(now);

        var newSession = new AuthSession
        {
            SessionId = newSessionId,
            UserId = current.UserId,
            ContextId = current.ContextId, // aynı sekme → aynı context
            Email = current.Email,
            UserName = current.UserName,
            ScopeLevel = command.TargetLevel,
            TenantId = command.TargetTenantId,
            CompanyId = command.TargetCompanyId,
            UnitId = command.TargetUnitId,
            Roles = roles,
            Permissions = permissions,
            PersonaSide = current.PersonaSide,
            IssuedAt = now,
            LastActivity = now,
        };

        var sessionTtl = TimeSpan.FromMinutes(_jwtSettings.AccessTokenMinutes + _sessionSettings.TtlPaddingMinutes);
        await _sessionStore.StoreAsync(newSession, sessionTtl, cancellationToken);

        // Eski session sil
        await _sessionStore.DeleteAsync(current.SessionId, current.UserId, cancellationToken);

        // Refresh token rotation — eski zinciri revoke, yeni token
        await _refreshTokenService.RevokeChainAsync(
            current.UserId, current.ContextId, "ContextSwitch", cancellationToken);
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
