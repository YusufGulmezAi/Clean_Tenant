using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Support;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using MediatR;

namespace CleanTenant.Application.Features.System.ImpersonateUser;

/// <summary>
/// <para>
/// Support Mode'da hedef kullanıcıya bürünme akışı:
/// </para>
/// <list type="number">
///   <item>Mevcut session aktif Support Mode olmalı (policy doğrular).</item>
///   <item>Hedef kullanıcı bulunmalı.</item>
///   <item>Hedef kullanıcının destek session'ın target tenant'ında ataması olmalı.</item>
///   <item>SupportSession kaydı: TargetUserId set + Mode=FullImpersonation.</item>
///   <item>Yeni Redis session: JWT'nin sub'u hedef kullanıcı; <c>ImpersonatedBy</c>=operatör.</item>
///   <item>Yeni JWT döner.</item>
/// </list>
/// </summary>
public sealed class ImpersonateUserCommandHandler : IRequestHandler<ImpersonateUserCommand, Result<TokenPair>>
{
    private readonly ICatalogDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthSessionStore _sessionStore;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IClock _clock;
    private readonly SessionSettings _sessionSettings;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ImpersonateUserCommandHandler(
        ICatalogDbContext db,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IAuthSessionStore sessionStore,
        ICurrentSessionAccessor sessionAccessor,
        IClock clock,
        IOptions<SessionSettings> sessionOptions)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _sessionStore = sessionStore;
        _sessionAccessor = sessionAccessor;
        _clock = clock;
        _sessionSettings = sessionOptions.Value;
    }

    /// <summary>Impersonate isteğini işler.</summary>
    public async Task<Result<TokenPair>> Handle(
        ImpersonateUserCommand command,
        CancellationToken cancellationToken)
    {
        var current = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        if (current.SupportSessionId is null || current.TenantId is null)
        {
            return Result<TokenPair>.Failure(
                Error.Failure("SUP-009", "Aktif Support Mode oturumu gerekli."));
        }

        // Hedef kullanıcı
        var target = await _db.Users
            .AsNoTracking()
            .Where(u => u.UrlCode == command.TargetUserUrlCode)
            .Select(u => new { u.Id, u.Email, u.UserName })
            .FirstOrDefaultAsync(cancellationToken);
        if (target is null)
        {
            return Result<TokenPair>.Failure(
                Error.NotFound("SUP-010", "Hedef kullanıcı bulunamadı."));
        }

        // Hedef kullanıcının target tenant içinde ataması olmalı
        var assignment = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == target.Id
                     && a.IsActive
                     && a.TenantId == current.TenantId)
            .OrderBy(a => a.ScopeLevel)
            .Select(a => new { a.ScopeLevel, a.TenantId, a.CompanyId, a.UnitId })
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return Result<TokenPair>.Failure(
                Error.Forbidden("SUP-011",
                    "Hedef kullanıcının bu tenant'ta aktif ataması yok; impersonate edilemez."));
        }

        // Hedef kullanıcının bu scope'taki rolleri + permission'ları
        var roles = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == target.Id
                     && a.IsActive
                     && a.ScopeLevel == assignment.ScopeLevel
                     && a.TenantId == assignment.TenantId
                     && a.CompanyId == assignment.CompanyId
                     && a.UnitId == assignment.UnitId)
            .Join(_db.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name!)
            .ToListAsync(cancellationToken);

        var permissions = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == target.Id
                     && a.IsActive
                     && a.ScopeLevel == assignment.ScopeLevel
                     && a.TenantId == assignment.TenantId
                     && a.CompanyId == assignment.CompanyId
                     && a.UnitId == assignment.UnitId)
            .Join(_db.RolePermissions, a => a.RoleId, rp => rp.RoleId, (a, rp) => rp.PermissionId)
            .Join(_db.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        // SupportSession güncelle
        var support = await _db.SupportSessions
            .FirstOrDefaultAsync(s => s.Id == current.SupportSessionId.Value, cancellationToken);
        if (support is null)
        {
            return Result<TokenPair>.Failure(
                Error.NotFound("SUP-012", "SupportSession DB kaydı bulunamadı."));
        }
        support.TargetUserId = target.Id;
        support.Mode = SupportSessionMode.FullImpersonation;
        await _db.SaveChangesAsync(cancellationToken);

        // Operatörün support session'ını sil; yeni impersonation session yarat
        var operatorUserId = current.UserId;
        await _sessionStore.DeleteAsync(current.SessionId, operatorUserId, cancellationToken);

        var now = _clock.UtcNow;
        var newSessionId = Guid.CreateVersion7(now);
        var impSession = new AuthSession
        {
            SessionId = newSessionId,
            UserId = target.Id,
            ContextId = current.ContextId,
            Email = target.Email ?? string.Empty,
            UserName = target.UserName ?? target.Email ?? string.Empty,
            ScopeLevel = assignment.ScopeLevel,
            TenantId = assignment.TenantId,
            CompanyId = assignment.CompanyId,
            UnitId = assignment.UnitId,
            Roles = roles,
            Permissions = permissions,
            PersonaSide = current.PersonaSide,
            IsSystemSession = true,
            SupportSessionId = current.SupportSessionId,
            SupportMode = "FullImpersonation",
            OriginalSessionId = current.OriginalSessionId, // exit'te operatöre geri dönülecek
            ImpersonatedBy = operatorUserId,
            IssuedAt = now,
            LastActivity = now,
        };

        var impTtl = TimeSpan.FromMinutes(10 + _sessionSettings.TtlPaddingMinutes);
        await _sessionStore.StoreAsync(impSession, impTtl, cancellationToken);

        // Yeni refresh token (operatör userId üzerinden — refresh token operatöre bağlı)
        var (rawRefresh, refreshExpiry) = await _refreshTokenService.CreateAsync(
            operatorUserId, current.ContextId, command.IpAddress, command.UserAgent, cancellationToken);

        var jwt = _jwtTokenService.IssueToken(impSession);

        return Result<TokenPair>.Success(new TokenPair(
            jwt.Token,
            jwt.ExpiresAt,
            rawRefresh,
            refreshExpiry,
            newSessionId,
            current.ContextId,
            new ScopeOption(assignment.ScopeLevel, assignment.TenantId, assignment.CompanyId, assignment.UnitId),
            AvailableScopes: []));
    }
}
