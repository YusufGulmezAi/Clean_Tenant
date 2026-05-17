using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.ConfirmTotpEnrollment;

/// <summary>
/// TOTP enrollment'ı onaylar:
/// <list type="number">
///   <item>Kullanıcının verdiği kod authenticator key ile doğrulanır.</item>
///   <item>Doğru ise <c>TwoFactorEnabled=true</c>.</item>
///   <item>10 adet tek kullanımlık recovery code üretilir, döner.</item>
/// </list>
/// </summary>
public sealed class ConfirmTotpEnrollmentCommandHandler
{
    private const string AuthenticatorProvider = "Authenticator";
    private const int RecoveryCodeCount = 10;

    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ConfirmTotpEnrollmentCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Confirm isteğini işler.</summary>
    public async Task<Result<ConfirmTotpEnrollmentResult>> HandleAsync(
        ConfirmTotpEnrollmentCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return Result<ConfirmTotpEnrollmentResult>.Failure(
                Error.Validation("AUTH-2FA-004", "Doğrulama kodu zorunlu."));
        }

        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result<ConfirmTotpEnrollmentResult>.Failure(
                Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<ConfirmTotpEnrollmentResult>.Failure(
                Error.Failure("AUTH-2FA-NOT-ENROLLED",
                    "Önce /api/v1/auth/2fa/enroll/totp ile enrollment başlatın."));
        }

        var valid = await _userManager.VerifyTwoFactorTokenAsync(user, AuthenticatorProvider, command.Code);
        if (!valid)
        {
            return Result<ConfirmTotpEnrollmentResult>.Failure(
                Error.Unauthorized("AUTH-2FA-INVALID-CODE", "Kod hatalı. Authenticator app saatinizin doğru olduğundan emin olun."));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        var recovery = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);
        var codes = recovery?.ToList() ?? [];

        return Result<ConfirmTotpEnrollmentResult>.Success(
            new ConfirmTotpEnrollmentResult(codes));
    }
}
