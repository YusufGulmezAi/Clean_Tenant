using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.EmailMethod;

/// <summary>E-posta doğrulama kodunu kontrol eder ve e-postayı onaylanmış işaretler.</summary>
public sealed class ConfirmEmailVerificationCommandHandler
    : IRequestHandler<ConfirmEmailVerificationCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ConfirmEmailVerificationCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>İsteği işler.</summary>
    public async Task<Result> Handle(ConfirmEmailVerificationCommand command, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user, TwoFactorDefaults.EmailMethod, command.Code.Trim());
        if (!valid)
        {
            return Result.Failure(
                Error.Unauthorized("AUTH-2FA-INVALID-CODE", "Kod hatalı veya süresi dolmuş."));
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                return Result.Failure(
                    Error.Failure("AUTH-2FA-EMAIL-CONFIRM-FAILED", "E-posta doğrulaması kaydedilemedi."));
            }
        }

        return Result.Success();
    }
}
