using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TenantStatus = CleanTenant.Domain.Identity.Tenants.TenantStatus;

namespace CleanTenant.Application.Features.Auth.Tenants;

/// <summary>
/// <see cref="SwitchTenantCommand"/> handler'ı.
/// <list type="number">
///   <item>Mevcut session'dan kullanıcıyı oku.</item>
///   <item>Hedef tenant'ı yükle + Active mi doğrula.</item>
///   <item>Yetki kontrolü: System scope ise her tenant'a izin; alt scope ise UserRoleAssignments'ta o tenant'a rol ataması olmalı.</item>
///   <item>Permissions/roles: System ise current session'dan miras; alt scope ise hedef tenant'taki en üst rol atamasından (öncelik Tenant > Company).</item>
///   <item>Yeni AuthSession Redis'e yaz, eski sil. Refresh chain revoke + yeni token. JWT döner.</item>
/// </list>
/// </summary>
public sealed class SwitchTenantCommandHandler : IRequestHandler<SwitchTenantCommand, Result<TokenPair>>
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
    public SwitchTenantCommandHandler(
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

    /// <summary>Switch-tenant isteğini işler.</summary>
    public async Task<Result<TokenPair>> Handle(SwitchTenantCommand command, CancellationToken cancellationToken)
    {
        var current = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null. Endpoint Bearer/Cookie korumalı olmalı.");

        // Persona kontrolü: Management persona Tenant'a geçebilir; Portal değil.
        if (current.PersonaSide != PersonaSide.Management)
        {
            return Result<TokenPair>.Failure(
                Error.Forbidden("AUTH-010",
                    $"Persona '{current.PersonaSide}' tenant geçişi yapamaz — Portal yalnız Unit scope'unda çalışır."));
        }

        // Hedef tenant Active mi?
        var tenant = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == command.TargetTenantId && t.Status == TenantStatus.Active)
            .Select(t => new { t.Id, t.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
        {
            return Result<TokenPair>.Failure(
                Error.NotFound("AUTH-TENANT-NOT-FOUND", "Tenant bulunamadı veya aktif değil."));
        }

        var isSystemUser = current.ScopeLevel == ScopeLevel.System;

        // System olmayan kullanıcı yalnız kendi rol atamasında olan tenant'a geçebilir
        if (!isSystemUser)
        {
            var hasAssignment = await _db.UserRoleAssignments.AsNoTracking()
                .AnyAsync(a => a.UserId == current.UserId
                            && a.IsActive
                            && a.TenantId == command.TargetTenantId,
                    cancellationToken);
            if (!hasAssignment)
            {
                return Result<TokenPair>.Failure(
                    Error.Forbidden("AUTH-011", "Bu tenant için aktif atama yok."));
            }
        }

        // Roller + permissions
        List<string> roles;
        List<string> permissions;
        if (isSystemUser)
        {
            // System operatörün cross-tenant erişimi — system permissions korunur.
            // Audit zinciri (Faz 1.6 Support Mode entegrasyonu) bu geçişi kaydeder;
            // şimdilik AuthSession üzerinden permission set'i system'den miras.
            roles = [.. current.Roles];
            permissions = [.. current.Permissions];
        }
        else
        {
            // Hedef tenant'taki en uygun rol ataması (Tenant > Company > Unit önceliği)
            var assignments = await _db.UserRoleAssignments.AsNoTracking()
                .Where(a => a.UserId == current.UserId
                         && a.IsActive
                         && a.TenantId == command.TargetTenantId)
                .ToListAsync(cancellationToken);

            // En geniş scope (öncelik Tenant)
            var primary = assignments
                .OrderBy(a => a.ScopeLevel switch
                {
                    ScopeLevel.Tenant => 0,
                    ScopeLevel.Company => 1,
                    ScopeLevel.Unit => 2,
                    _ => 99,
                })
                .First();

            roles = await _db.UserRoleAssignments.AsNoTracking()
                .Where(a => a.UserId == current.UserId
                         && a.IsActive
                         && a.TenantId == command.TargetTenantId
                         && a.ScopeLevel == primary.ScopeLevel
                         && a.CompanyId == primary.CompanyId
                         && a.UnitId == primary.UnitId)
                .Join(_db.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name!)
                .ToListAsync(cancellationToken);

            permissions = await _db.UserRoleAssignments.AsNoTracking()
                .Where(a => a.UserId == current.UserId
                         && a.IsActive
                         && a.TenantId == command.TargetTenantId
                         && a.ScopeLevel == primary.ScopeLevel
                         && a.CompanyId == primary.CompanyId
                         && a.UnitId == primary.UnitId)
                .Join(_db.RolePermissions, a => a.RoleId, rp => rp.RoleId, (a, rp) => rp.PermissionId)
                .Join(_db.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        var now = _clock.UtcNow;
        var newSessionId = Guid.CreateVersion7(now);

        var newSession = new AuthSession
        {
            SessionId = newSessionId,
            UserId = current.UserId,
            ContextId = current.ContextId, // aynı sekme → aynı context
            Email = current.Email,
            UserName = current.UserName,
            FullName = current.FullName,
            ScopeLevel = isSystemUser ? ScopeLevel.System : ScopeLevel.Tenant,
            TenantId = command.TargetTenantId,
            TenantName = tenant.Name,
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
            current.UserId, current.ContextId, "TenantSwitch", cancellationToken);
        var (rawRefresh, refreshExpiry) = await _refreshTokenService.CreateAsync(
            current.UserId, current.ContextId, command.IpAddress, command.UserAgent, cancellationToken);

        var jwt = _jwtTokenService.IssueToken(newSession);

        var currentScope = new ScopeOption(
            newSession.ScopeLevel,
            newSession.TenantId,
            newSession.CompanyId,
            newSession.UnitId,
            TenantName: tenant.Name);

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
