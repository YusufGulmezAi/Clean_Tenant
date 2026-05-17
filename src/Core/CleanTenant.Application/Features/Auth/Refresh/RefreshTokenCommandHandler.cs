using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using Microsoft.Extensions.Options;

using MediatR;

namespace CleanTenant.Application.Features.Auth.Refresh;

/// <summary>
/// <para>
/// Refresh token akışını yürütür: rotation chain doğrulama, replay tespiti,
/// aynı session ID üzerinde yeni JWT üretme, eski refresh'i revoke + yenisini
/// kayıt etme.
/// </para>
/// <para>
/// <b>Session davranışı:</b> Refresh sırasında Redis session devam eder
/// (aynı <c>sessionId</c>). TTL sliding olarak uzatılır.
/// </para>
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenPair>>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthSessionStore _sessionStore;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IClock _clock;
    private readonly JwtSettings _jwtSettings;
    private readonly SessionSettings _sessionSettings;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        IAuthSessionStore sessionStore,
        IJwtTokenService jwtTokenService,
        IClock clock,
        IOptions<JwtSettings> jwtOptions,
        IOptions<SessionSettings> sessionOptions)
    {
        _refreshTokenService = refreshTokenService;
        _sessionStore = sessionStore;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _jwtSettings = jwtOptions.Value;
        _sessionSettings = sessionOptions.Value;
    }

    /// <summary>Refresh isteğini işler ve yeni token çiftini döner.</summary>
    public async Task<Result<TokenPair>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var rotation = await _refreshTokenService.RotateAsync(
            command.RefreshToken, command.IpAddress, command.UserAgent, cancellationToken);

        if (!rotation.IsValid)
        {
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-006", rotation.ErrorMessage ?? "Refresh token geçersiz."));
        }

        // Refresh sırasında session devam eder; ama Redis'te halen var olduğunu doğrulamalıyız.
        // Refresh token DB'de geçerliyken Redis session'ı silinmişse (örn. logout) kullanıcıya yeni token
        // verilmemeli.
        // Pragmatik: refresh sırasında yeni session yarat (eski session zaten silinmiş varsayımıyla),
        // session lookup middleware doğrulayacak.
        // Daha kesin: session id'yi refresh token kaydından alıp Redis'te varsa devam, yoksa hata.
        // v0.1.5.a için yeni session üretiyoruz; v0.1.5.b'de session-refresh köprüsü olgunlaşacak.

        var now = _clock.UtcNow;
        var newSessionId = Guid.CreateVersion7(now);

        // Var olan session'ı bulamadığımız için minimum bir AuthSession üretiyoruz.
        // Production akışında refresh token kaydı session'ı taşır; bu detay v0.1.5.b'de zenginleşecek.
        // Geçici: refresh sonrası kullanıcı yine login yapsın gerekiyorsa, refresh akışı session
        // lookup'a göre genişletilir. Şimdilik mevcut session'la devam edilemiyorsa hata döneriz.
        var existingSessions = await _sessionStore.GetActiveSessionIdsForUserAsync(rotation.UserId, cancellationToken);
        AuthSession? sessionToReuse = null;
        foreach (var sid in existingSessions)
        {
            var s = await _sessionStore.GetAsync(sid, cancellationToken);
            if (s is not null && s.ContextId == rotation.ContextId)
            {
                sessionToReuse = s;
                break;
            }
        }

        if (sessionToReuse is null)
        {
            // Session bulunamadı (TTL doldu veya logout) → kullanıcı tekrar login olmalı.
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-007", "Oturum sona erdi; lütfen tekrar giriş yapın."));
        }

        // TTL'yi yenile (sliding)
        var sessionTtl = TimeSpan.FromMinutes(_jwtSettings.AccessTokenMinutes + _sessionSettings.TtlPaddingMinutes);
        await _sessionStore.TouchAsync(sessionToReuse.SessionId, sessionTtl, cancellationToken);

        // Yeni JWT
        var jwt = _jwtTokenService.IssueToken(sessionToReuse);

        // Refresh response'unda availableScopes boş döner — istemci login zamanı
        // cache'lediği listeyi kullanır. CurrentScope session'dan türetilir.
        var currentScope = new ScopeOption(
            sessionToReuse.ScopeLevel,
            sessionToReuse.TenantId,
            sessionToReuse.CompanyId,
            sessionToReuse.UnitId);

        return Result<TokenPair>.Success(new TokenPair(
            jwt.Token,
            jwt.ExpiresAt,
            rotation.NewRawToken!,
            rotation.NewExpiresAt,
            sessionToReuse.SessionId,
            sessionToReuse.ContextId,
            currentScope,
            AvailableScopes: []));
    }
}
