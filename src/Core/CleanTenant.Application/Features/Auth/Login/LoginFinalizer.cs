using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanTenant.Application.Features.Auth.Login;

/// <summary>
/// <para>
/// Şifre + (gerekiyorsa) 2FA doğrulamasından <em>sonra</em> ortak akışı yürüten servis:
/// scope seçimi, role + permission yükleme, Redis session + JWT + refresh token üretimi,
/// <see cref="User.LastLoginAt"/>/<see cref="User.LastLoginIp"/> güncellemesi.
/// </para>
/// <para>
/// Hem <see cref="LoginCommandHandler"/> (2FA'sız hesaplar için) hem
/// <see cref="TwoFactor.VerifyTwoFactor.VerifyTwoFactorCommandHandler"/> (2FA'lı hesaplarda
/// challenge doğrulandıktan sonra) bunu çağırır.
/// </para>
/// </summary>
public sealed class LoginFinalizer
{
    private readonly UserManager<User> _userManager;
    private readonly ICatalogDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthSessionStore _sessionStore;
    private readonly IClock _clock;
    private readonly JwtSettings _jwtSettings;
    private readonly SessionSettings _sessionSettings;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LoginFinalizer(
        UserManager<User> userManager,
        ICatalogDbContext db,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IAuthSessionStore sessionStore,
        IClock clock,
        IOptions<JwtSettings> jwtOptions,
        IOptions<SessionSettings> sessionOptions)
    {
        _userManager = userManager;
        _db = db;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _sessionStore = sessionStore;
        _clock = clock;
        _jwtSettings = jwtOptions.Value;
        _sessionSettings = sessionOptions.Value;
    }

    /// <summary>
    /// Persona ile filtrelenmiş scope'ları üretir, primary'i seçer, session açar,
    /// JWT + refresh token döner. Persona için scope yoksa <c>AUTH-004</c> hatası.
    /// </summary>
    public async Task<Result<TokenPair>> FinalizeAsync(
        User user,
        PersonaSide persona,
        Guid? requestedContextId,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var allOptions = await BuildScopeOptionsAsync(user.Id, cancellationToken);
        var availableScopes = ScopeSelector.FilterByPersona(allOptions, persona).ToList();
        if (availableScopes.Count == 0)
        {
            return Result<TokenPair>.Failure(
                Error.Forbidden("AUTH-004",
                    $"Bu kullanıcı için {persona} persona'sında erişilebilir bir scope yok."));
        }

        var primary = ScopeSelector.SelectPrimary(availableScopes, persona)!;
        var (roles, permissions) = await LoadRolesAndPermissionsAsync(user.Id, primary, cancellationToken);

        var now = _clock.UtcNow;
        var contextId = requestedContextId ?? Guid.CreateVersion7(now);
        var sessionId = Guid.CreateVersion7(now);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var session = new AuthSession
        {
            SessionId = sessionId,
            UserId = user.Id,
            ContextId = contextId,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? user.Email ?? string.Empty,
            FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
            ScopeLevel = primary.Level,
            TenantId = primary.TenantId,
            TenantName = primary.TenantName,
            CompanyId = primary.CompanyId,
            UnitId = primary.UnitId,
            Roles = roles,
            Permissions = permissions,
            PersonaSide = persona,
            IssuedAt = now,
            LastActivity = now,
        };

        var sessionTtl = TimeSpan.FromMinutes(_jwtSettings.AccessTokenMinutes + _sessionSettings.TtlPaddingMinutes);
        await _sessionStore.StoreAsync(session, sessionTtl, cancellationToken);

        var jwt = _jwtTokenService.IssueToken(session);
        var (rawRefresh, refreshExpiry) = await _refreshTokenService.CreateAsync(
            user.Id, contextId, ipAddress, userAgent, cancellationToken);

        user.LastLoginAt = now;
        user.LastLoginIp = ipAddress;
        await _userManager.UpdateAsync(user);

        return Result<TokenPair>.Success(new TokenPair(
            jwt.Token,
            jwt.ExpiresAt,
            rawRefresh,
            refreshExpiry,
            sessionId,
            contextId,
            primary,
            availableScopes));
    }

    /// <summary>
    /// Kullanıcının System scope'ta aktif rol ataması var mı kontrol eder.
    /// 2FA enrollment-required check'i için kullanılır (yalnız System için zorunlu).
    /// </summary>
    public Task<bool> HasSystemScopeAsync(Guid userId, CancellationToken cancellationToken) =>
        _db.UserRoleAssignments
            .AsNoTracking()
            .AnyAsync(a => a.UserId == userId && a.IsActive && a.ScopeLevel == ScopeLevel.System,
                cancellationToken);

    private async Task<List<ScopeOption>> BuildScopeOptionsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var scopes = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive)
            .Select(a => new { a.ScopeLevel, a.TenantId, a.CompanyId, a.UnitId })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (scopes.Count == 0)
        {
            return [];
        }

        var tenantIds = scopes.Where(s => s.TenantId.HasValue).Select(s => s.TenantId!.Value).Distinct().ToList();
        var tenantNames = await _db.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        return [.. scopes.Select(s => new ScopeOption(
            s.ScopeLevel,
            s.TenantId,
            s.CompanyId,
            s.UnitId,
            s.TenantId.HasValue && tenantNames.TryGetValue(s.TenantId.Value, out var name) ? name : null))];
    }

    private async Task<(List<string> Roles, List<string> Permissions)> LoadRolesAndPermissionsAsync(
        Guid userId,
        ScopeOption scope,
        CancellationToken cancellationToken)
    {
        var roles = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                     && a.IsActive
                     && a.ScopeLevel == scope.Level
                     && a.TenantId == scope.TenantId
                     && a.CompanyId == scope.CompanyId
                     && a.UnitId == scope.UnitId)
            .Join(_db.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name!)
            .ToListAsync(cancellationToken);

        var permissions = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                     && a.IsActive
                     && a.ScopeLevel == scope.Level
                     && a.TenantId == scope.TenantId
                     && a.CompanyId == scope.CompanyId
                     && a.UnitId == scope.UnitId)
            .Join(_db.RolePermissions, a => a.RoleId, rp => rp.RoleId, (a, rp) => rp.PermissionId)
            .Join(_db.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return (roles, permissions);
    }
}
