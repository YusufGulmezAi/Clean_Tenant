using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
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
/// Login akışını yürüten handler. Adımlar:
/// </para>
/// <list type="number">
///   <item>UserManager ile kullanıcı bul + lockout kontrol.</item>
///   <item>Şifre doğrulama.</item>
///   <item>2FA aktifse şimdilik reddet (v0.1.5.c).</item>
///   <item>Kullanıcının aktif rol atamalarını çek; ilkini scope olarak seç.</item>
///   <item>Permission listesini DB'den oluştur.</item>
///   <item>Yeni session'ı Redis'e yaz.</item>
///   <item>JWT + refresh token üret, döndür.</item>
/// </list>
/// </summary>
public sealed class LoginCommandHandler
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
    public LoginCommandHandler(
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

    /// <summary>Login isteğini işler ve sonucu döner.</summary>
    public async Task<Result<TokenPair>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        // Girdi doğrulaması (FluentValidation v0.1.6'da; şimdilik inline)
        if (string.IsNullOrWhiteSpace(command.Identifier) || string.IsNullOrWhiteSpace(command.Password))
        {
            return Result<TokenPair>.Failure(
                Error.Validation("AUTH-001", "Kullanıcı kimliği ve şifre zorunlu."));
        }

        var (idType, normalized) = LoginIdentifier.Resolve(command.Identifier);
        if (idType == LoginIdentifierType.Unknown)
        {
            return Result<TokenPair>.Failure(
                Error.Validation("AUTH-009",
                    "Identifier tanınamadı — geçerli bir e-posta, TCKN veya cep telefonu girin."));
        }

        var user = await ResolveUserAsync(idType, normalized, cancellationToken);
        if (user is null)
        {
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-002", "Geçersiz kimlik veya şifre."));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-003", "Hesap geçici olarak kilitli."));
        }

        var passwordOk = await _userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordOk)
        {
            await _userManager.AccessFailedAsync(user);
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-002", "Geçersiz e-posta veya şifre."));
        }

        // 2FA v0.1.5.c'de devreye girecek; şu an aktifse reddet.
        if (user.TwoFactorEnabled)
        {
            return Result<TokenPair>.Failure(
                Error.Failure("AUTH-2FA-REQUIRED", "Bu hesap için 2FA doğrulaması henüz aktif değil (v0.1.5.c)."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        // Kullanıcının tüm aktif atamalarını çek (Role + Tenant + Company isimleriyle zenginleştir)
        var allOptions = await BuildScopeOptionsAsync(user.Id, cancellationToken);

        // Persona'ya göre filtrele
        var availableScopes = ScopeSelector.FilterByPersona(allOptions, command.Persona).ToList();
        if (availableScopes.Count == 0)
        {
            return Result<TokenPair>.Failure(
                Error.Forbidden("AUTH-004",
                    $"Bu kullanıcı için {command.Persona} persona'sında erişilebilir bir scope yok."));
        }

        // Primary scope seçimi
        var primary = ScopeSelector.SelectPrimary(availableScopes, command.Persona)!;

        // Primary scope'taki roller ve permission'lar
        var (roles, permissions) = await LoadRolesAndPermissionsAsync(user.Id, primary, cancellationToken);

        var now = _clock.UtcNow;
        var contextId = command.ContextId ?? Guid.CreateVersion7(now);
        var sessionId = Guid.CreateVersion7(now);

        var session = new AuthSession
        {
            SessionId = sessionId,
            UserId = user.Id,
            ContextId = contextId,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? user.Email ?? string.Empty,
            ScopeLevel = primary.Level,
            TenantId = primary.TenantId,
            CompanyId = primary.CompanyId,
            UnitId = primary.UnitId,
            Roles = roles,
            Permissions = permissions,
            PersonaSide = command.Persona,
            IssuedAt = now,
            LastActivity = now,
        };

        var sessionTtl = TimeSpan.FromMinutes(_jwtSettings.AccessTokenMinutes + _sessionSettings.TtlPaddingMinutes);
        await _sessionStore.StoreAsync(session, sessionTtl, cancellationToken);

        var jwt = _jwtTokenService.IssueToken(session);

        var (rawRefresh, refreshExpiry) = await _refreshTokenService.CreateAsync(
            user.Id, contextId, command.IpAddress, command.UserAgent, cancellationToken);

        user.LastLoginAt = now;
        user.LastLoginIp = command.IpAddress;
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
    /// Kullanıcının tüm aktif atamalarını <see cref="ScopeOption"/> listesine
    /// dönüştürür. Aynı scope (TenantId+CompanyId+UnitId) birden çok atama
    /// taşıyabilir (örn. Company X'te hem Manager hem Accountant) → tek seçenek olarak gruplanır.
    /// Tenant/Company adları lookup ile doldurulur (UI gösterimi için).
    /// </summary>
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

        // CompanyName / UnitLabel lookup'ları Faz 1'de Company/Unit entity'leri eklendiğinde
        // genişletilecek; v0.1.5.b'de yalnız Tenant adı.

        return [.. scopes.Select(s => new ScopeOption(
            s.ScopeLevel,
            s.TenantId,
            s.CompanyId,
            s.UnitId,
            s.TenantId.HasValue && tenantNames.TryGetValue(s.TenantId.Value, out var name) ? name : null))];
    }

    /// <summary>
    /// Verilen primary scope için aktif rolleri ve onlardan türetilen permission'ları yükler.
    /// </summary>
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

    /// <summary>
    /// Identifier tipine göre uygun lookup'ı yapar.
    /// TCKN → <c>TcknVerified=true</c> şartı, telefon → <c>PhoneNumberConfirmed=true</c>
    /// şartı (güvenlik).
    /// </summary>
    private async Task<User?> ResolveUserAsync(
        LoginIdentifierType idType,
        string normalized,
        CancellationToken cancellationToken)
    {
        return idType switch
        {
            LoginIdentifierType.Email
                => await _userManager.FindByEmailAsync(normalized),

            LoginIdentifierType.Tckn
                => await _db.Users
                    .FirstOrDefaultAsync(u => u.Tckn == normalized && u.TcknVerified, cancellationToken),

            LoginIdentifierType.Vkn
                => await _db.Users
                    .FirstOrDefaultAsync(u => u.Vkn == normalized && u.VknVerified, cancellationToken),

            LoginIdentifierType.PhoneNumber
                => await _db.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == normalized && u.PhoneNumberConfirmed, cancellationToken),

            _ => null,
        };
    }
}
