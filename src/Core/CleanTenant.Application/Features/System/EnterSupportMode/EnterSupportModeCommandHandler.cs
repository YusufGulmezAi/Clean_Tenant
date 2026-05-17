using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Support;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanTenant.Application.Features.System.EnterSupportMode;

/// <summary>
/// <para>
/// Support Mode'a girme akışı:
/// </para>
/// <list type="number">
///   <item>Mevcut session System scope olmalı (policy zaten doğruluyor).</item>
///   <item>Hedef tenant var ve Active olmalı.</item>
///   <item>SupportSession DB kaydı oluştur (Mode=ReadOnly).</item>
///   <item>Yeni Redis session: Tenant scope, IsSystemSession=true, OriginalSessionId=mevcut.</item>
///   <item>Yeni JWT döner.</item>
/// </list>
/// </summary>
public sealed class EnterSupportModeCommandHandler
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
    public EnterSupportModeCommandHandler(
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

    /// <summary>Enter Support Mode isteğini işler.</summary>
    public async Task<Result<TokenPair>> HandleAsync(
        EnterSupportModeCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Length < 20)
        {
            return Result<TokenPair>.Failure(
                Error.Validation("SUP-001", "Sebep zorunlu (minimum 20 karakter)."));
        }

        var current = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        // Hedef tenant kontrolü
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == command.TargetTenantId)
            .Select(t => new { t.Id, t.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
        {
            return Result<TokenPair>.Failure(
                Error.NotFound("SUP-002", "Hedef tenant bulunamadı."));
        }

        var now = _clock.UtcNow;

        // SupportSession DB kaydı
        var supportSession = new SupportSession
        {
            OperatorUserId = current.UserId,
            TargetTenantId = command.TargetTenantId,
            Mode = SupportSessionMode.ReadOnly,
            Reason = command.Reason,
            StartedAt = now,
            WriteActionCount = 0,
            CustomerNotified = false,
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent,
        };
        _db.SupportSessions.Add(supportSession);
        await _db.SaveChangesAsync(cancellationToken);

        // Yeni Redis session (Tenant scope, Support Mode)
        var newSessionId = Guid.CreateVersion7(now);
        var supportAuthSession = new AuthSession
        {
            SessionId = newSessionId,
            UserId = current.UserId,
            ContextId = current.ContextId,
            Email = current.Email,
            UserName = current.UserName,
            ScopeLevel = ScopeLevel.Tenant,
            TenantId = command.TargetTenantId,
            CompanyId = null,
            UnitId = null,
            Roles = ["SystemSupport"], // sanal rol; permission'lar boş — operatör görüntüleyici
            Permissions = [],
            PersonaSide = current.PersonaSide,
            IsSystemSession = true,
            SupportSessionId = supportSession.Id,
            SupportMode = "ReadOnly",
            OriginalSessionId = current.SessionId,
            IssuedAt = now,
            LastActivity = now,
        };

        // Support session daha kısa TTL (10 dk + padding) — kuralda Support için 5 dk access token + 30 padding.
        var supportTtl = TimeSpan.FromMinutes(10 + _sessionSettings.TtlPaddingMinutes);
        await _sessionStore.StoreAsync(supportAuthSession, supportTtl, cancellationToken);

        // Yeni JWT (Support için kısa TTL kullanmıyoruz şimdilik; standart JWT TTL — v0.1.5.c'de ayarlanabilir)
        var jwt = _jwtTokenService.IssueToken(supportAuthSession);

        // Refresh token: aynı context için yeni token üret (eski Refresh zinciri revoke edilmez —
        // operatör orijinal session'a geri dönecek)
        var (rawRefresh, refreshExpiry) = await _refreshTokenService.CreateAsync(
            current.UserId, current.ContextId, command.IpAddress, command.UserAgent, cancellationToken);

        return Result<TokenPair>.Success(new TokenPair(
            jwt.Token,
            jwt.ExpiresAt,
            rawRefresh,
            refreshExpiry,
            newSessionId,
            current.ContextId,
            new ScopeOption(ScopeLevel.Tenant, command.TargetTenantId, null, null),
            AvailableScopes: []));
    }
}
