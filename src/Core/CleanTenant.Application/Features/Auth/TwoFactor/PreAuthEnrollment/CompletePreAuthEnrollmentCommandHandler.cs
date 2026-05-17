using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PreAuthEnrollment;

/// <summary>
/// Pre-auth 2FA enrollment kod doğrulama + 2FA aktivasyon + recovery code üretim.
/// Challenge tüketilmez (silinmez) — finalize endpoint'i token'ı son adımda siler.
/// Idempotency: kod yanlışsa challenge canlı kalır, kullanıcı yeni kodla tekrar
/// dener; lockout sayacı artırılmaz (pre-auth aşaması, brute force koruması TTL).
/// </summary>
public sealed class CompletePreAuthEnrollmentCommandHandler
    : IRequestHandler<CompletePreAuthEnrollmentCommand, Result<CompletePreAuthEnrollmentResult>>
{
    private const string AuthenticatorProvider = "Authenticator";
    private const int RecoveryCodeCount = 10;

    private readonly IPreAuthEnrollmentStore _store;
    private readonly UserManager<User> _userManager;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CompletePreAuthEnrollmentCommandHandler(
        IPreAuthEnrollmentStore store,
        UserManager<User> userManager,
        IClock clock)
    {
        _store = store;
        _userManager = userManager;
        _clock = clock;
    }

    /// <summary>Complete isteğini işler.</summary>
    public async Task<Result<CompletePreAuthEnrollmentResult>> Handle(
        CompletePreAuthEnrollmentCommand command,
        CancellationToken cancellationToken)
    {
        var challenge = await _store.GetAsync(command.ChallengeToken, cancellationToken);
        if (challenge is null)
        {
            return Result<CompletePreAuthEnrollmentResult>.Failure(
                Error.Unauthorized("AUTH-2FA-ENROLL-CHALLENGE-NOT-FOUND",
                    "Enrollment süresi doldu veya geçersiz token. Lütfen tekrar giriş yapın."));
        }

        var user = await _userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user is null)
        {
            await _store.DeleteAsync(command.ChallengeToken, cancellationToken);
            return Result<CompletePreAuthEnrollmentResult>.Failure(
                Error.Unauthorized("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<CompletePreAuthEnrollmentResult>.Failure(
                Error.Failure("AUTH-2FA-NOT-ENROLLED",
                    "Önce enrollment akışını başlatın (Start adımı)."));
        }

        var valid = await _userManager.VerifyTwoFactorTokenAsync(user, AuthenticatorProvider, command.Code);
        if (!valid)
        {
            return Result<CompletePreAuthEnrollmentResult>.Failure(
                Error.Unauthorized("AUTH-2FA-INVALID-CODE",
                    "Kod hatalı. Authenticator app saatinizin doğru olduğundan emin olun."));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        var recovery = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);
        var codes = recovery?.ToList() ?? [];

        challenge.VerifiedAt = _clock.UtcNow;
        await _store.UpdateAsync(challenge, cancellationToken);

        return Result<CompletePreAuthEnrollmentResult>.Success(
            new CompletePreAuthEnrollmentResult(codes));
    }
}
