using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Auth.Login;

/// <summary>
/// <para>
/// Login akışını yürüten handler. Adımlar:
/// </para>
/// <list type="number">
///   <item>UserManager ile kullanıcı bul + lockout kontrol.</item>
///   <item>Şifre doğrulama.</item>
///   <item>2FA aktifse → <see cref="TwoFactorChallenge"/> üret ve istemciye yönlendir.</item>
///   <item>2FA aktif değil ama System scope rolü var → <see cref="PreAuthEnrollmentChallenge"/>
///   üret ve <see cref="LoginStatus.EnrollmentRequired"/> döndür (v0.2.2.a).</item>
///   <item>Diğer durum → <see cref="LoginFinalizer"/> ile TokenPair üret.</item>
/// </list>
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    /// <summary>2FA challenge için kısa TTL — istemcinin kod girmek için makul süresi.</summary>
    public static readonly TimeSpan TwoFactorChallengeTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Pre-auth enrollment challenge TTL. QR tarama + ilk kod + recovery code
    /// okuma + tamamlama akışına yetecek makul süre.
    /// </summary>
    public static readonly TimeSpan PreAuthEnrollmentChallengeTtl = TimeSpan.FromMinutes(10);

    private readonly UserManager<User> _userManager;
    private readonly ICatalogDbContext _db;
    private readonly LoginFinalizer _finalizer;
    private readonly ITwoFactorChallengeStore _challengeStore;
    private readonly IPreAuthEnrollmentStore _enrollmentStore;
    private readonly IPasswordChangeChallengeStore _passwordChangeStore;
    private readonly IAccountLockoutService _lockout;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LoginCommandHandler(
        UserManager<User> userManager,
        ICatalogDbContext db,
        LoginFinalizer finalizer,
        ITwoFactorChallengeStore challengeStore,
        IPreAuthEnrollmentStore enrollmentStore,
        IPasswordChangeChallengeStore passwordChangeStore,
        IAccountLockoutService lockout,
        IClock clock)
    {
        _userManager = userManager;
        _db = db;
        _finalizer = finalizer;
        _challengeStore = challengeStore;
        _enrollmentStore = enrollmentStore;
        _passwordChangeStore = passwordChangeStore;
        _lockout = lockout;
        _clock = clock;
    }

    /// <summary>Login isteğini işler ve sonucu (TokenPair veya 2FA challenge) döner.</summary>
    public async Task<Result<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var (idType, normalized) = LoginIdentifier.Resolve(command.Identifier);
        if (idType == LoginIdentifierType.Unknown)
        {
            return Result<LoginResult>.Failure(
                Error.Validation("AUTH-009",
                    "Identifier tanınamadı — geçerli bir e-posta, TCKN veya cep telefonu girin."));
        }

        var user = await ResolveUserAsync(idType, normalized, cancellationToken);
        if (user is null)
        {
            return Result<LoginResult>.Failure(
                Error.Unauthorized("AUTH-002", "Geçersiz kimlik veya şifre."));
        }

        if (!user.IsActive)
        {
            return Result<LoginResult>.Failure(
                Error.Unauthorized("AUTH-010", "Bu hesap devre dışı bırakılmış. Yöneticinizle iletişime geçin."));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            return LockedOut(lockoutEnd);
        }

        var passwordOk = await _userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordOk)
        {
            // Tenant-başına politikaya göre sayacı artır; eşik aşıldıysa kilitle.
            var lockedUntil = await _lockout.RegisterFailedAttemptAsync(user, cancellationToken);
            return lockedUntil is not null
                ? LockedOut(lockedUntil)
                : Result<LoginResult>.Failure(
                    Error.Unauthorized("AUTH-002", "Geçersiz kimlik veya şifre."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        // İlk giriş zorunlu şifre değişimi — 2FA'dan önce kontrol edilir
        if (user.RequiresPasswordChange)
        {
            return await IssuePasswordChangeChallengeAsync(user, command, cancellationToken);
        }

        // 2FA branching
        if (user.TwoFactorEnabled)
        {
            return await IssueTwoFactorChallengeAsync(user, command, cancellationToken);
        }

        // System scope rolü olup 2FA hiç açmamış kullanıcı → pre-auth enrollment challenge
        if (await _finalizer.HasSystemScopeAsync(user.Id, cancellationToken))
        {
            return await IssueEnrollmentChallengeAsync(user, command, cancellationToken);
        }

        // Normal akış
        var finalize = await _finalizer.FinalizeAsync(
            user, command.Persona, command.ContextId, command.IpAddress, command.UserAgent, cancellationToken);

        return finalize.IsSuccess
            ? Result<LoginResult>.Success(new LoginResult(LoginStatus.Success, finalize.Value, null))
            : Result<LoginResult>.Failure(finalize.FirstError);
    }

    /// <summary>
    /// <c>AUTH-003</c> kilit hatasını üretir. Kilit bitiş zamanı biliniyorsa,
    /// login ekranının canlı geri sayım gösterebilmesi için
    /// <c>Metadata["lockedUntil"]</c> (ISO 8601 / UTC) eklenir. Kalan deneme
    /// sayısı bilinçli olarak istemciye verilmez (güvenlik — enumeration).
    /// </summary>
    private static Result<LoginResult> LockedOut(DateTimeOffset? lockoutEnd)
    {
        var error = Error.Unauthorized("AUTH-003", "Hesap geçici olarak kilitli.");
        if (lockoutEnd is { } until)
        {
            error = error.WithMetadata(new Dictionary<string, string>
            {
                ["lockedUntil"] = until.ToUniversalTime().ToString("O"),
            });
        }

        return Result<LoginResult>.Failure(error);
    }

    private async Task<Result<LoginResult>> IssuePasswordChangeChallengeAsync(
        User user,
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromMinutes(15);
        var now = _clock.UtcNow;
        var challengeToken = Guid.CreateVersion7(now);
        var challenge = new PasswordChangeChallenge
        {
            ChallengeToken = challengeToken,
            UserId = user.Id,
            Email = user.Email ?? user.UserName ?? string.Empty,
            ContextId = command.ContextId ?? Guid.CreateVersion7(now),
            Persona = command.Persona,
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent,
            IssuedAt = now,
        };

        await _passwordChangeStore.StoreAsync(challenge, ttl, cancellationToken);

        var response = new PasswordChangeChallengeResponse(
            challengeToken,
            now.Add(ttl),
            challenge.Email);

        return Result<LoginResult>.Success(
            new LoginResult(LoginStatus.PasswordChangeRequired, null, null, null, response));
    }

    private async Task<Result<LoginResult>> IssueEnrollmentChallengeAsync(
        User user,
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var challengeToken = Guid.CreateVersion7(now);
        var challenge = new PreAuthEnrollmentChallenge
        {
            ChallengeToken = challengeToken,
            UserId = user.Id,
            Email = user.Email ?? user.UserName ?? string.Empty,
            ContextId = command.ContextId ?? Guid.CreateVersion7(now),
            Persona = command.Persona,
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent,
            IssuedAt = now,
            VerifiedAt = null,
        };

        await _enrollmentStore.StoreAsync(challenge, PreAuthEnrollmentChallengeTtl, cancellationToken);

        var response = new PreAuthEnrollmentChallengeResponse(
            challengeToken,
            now.Add(PreAuthEnrollmentChallengeTtl),
            challenge.Email);

        return Result<LoginResult>.Success(
            new LoginResult(LoginStatus.EnrollmentRequired, null, null, response));
    }

    private async Task<Result<LoginResult>> IssueTwoFactorChallengeAsync(
        User user,
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);
        if (providers.Count == 0)
        {
            // TwoFactorEnabled=true ama hiç yöntem yok (tutarsız durum); enrollment'a yönlendir.
            return Result<LoginResult>.Failure(
                Error.Failure("AUTH-2FA-ENROLLMENT-REQUIRED",
                    "2FA aktif görünüyor ama hiç doğrulama yöntemi tanımlı değil — tekrar enrollment yapın."));
        }

        var now = _clock.UtcNow;
        var challengeToken = Guid.CreateVersion7(now);
        var challenge = new TwoFactorChallenge
        {
            ChallengeToken = challengeToken,
            UserId = user.Id,
            ContextId = command.ContextId ?? Guid.CreateVersion7(now),
            Persona = command.Persona,
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent,
            IssuedAt = now,
            AvailableMethods = [.. providers],
        };

        await _challengeStore.StoreAsync(challenge, TwoFactorChallengeTtl, cancellationToken);

        var response = new TwoFactorChallengeResponse(
            challengeToken,
            now.Add(TwoFactorChallengeTtl),
            challenge.AvailableMethods);

        return Result<LoginResult>.Success(new LoginResult(LoginStatus.TwoFactorRequired, null, response));
    }

    /// <summary>
    /// Identifier tipine göre uygun lookup'ı yapar.
    /// TCKN → <c>TcknVerified=true</c> şartı, telefon → <c>PhoneNumberConfirmed=true</c> şartı (güvenlik).
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
