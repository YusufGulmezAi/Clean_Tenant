using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.RemoveMethod;

/// <summary>E-posta / telefon 2FA yöntemini son-metot kilidini gözeterek kaldırır.</summary>
public sealed class RemoveTwoFactorMethodCommandHandler : IRequestHandler<RemoveTwoFactorMethodCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly LoginFinalizer _finalizer;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveTwoFactorMethodCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor,
        LoginFinalizer finalizer)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
        _finalizer = finalizer;
    }

    /// <summary>İsteği işler.</summary>
    public async Task<Result> Handle(RemoveTwoFactorMethodCommand command, CancellationToken cancellationToken)
    {
        var isEmail = string.Equals(command.Method, TwoFactorDefaults.EmailMethod, StringComparison.OrdinalIgnoreCase);
        var isPhone = string.Equals(command.Method, TwoFactorDefaults.PhoneMethod, StringComparison.OrdinalIgnoreCase);
        if (!isEmail && !isPhone)
        {
            return Result.Failure(
                Error.Validation("AUTH-2FA-METHOD-INVALID",
                    "Yalnız 'Email' veya 'Phone' kaldırılabilir (Authenticator için DisableTotp)."));
        }

        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        var targetMethod = isEmail ? TwoFactorDefaults.EmailMethod : TwoFactorDefaults.PhoneMethod;
        var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);

        if (!providers.Any(p => string.Equals(p, targetMethod, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Success(); // Yöntem zaten yok — idempotent.
        }

        var hasOtherMethods = providers.Any(p =>
            !string.Equals(p, targetMethod, StringComparison.OrdinalIgnoreCase));

        if (!hasOtherMethods && await _finalizer.HasSystemScopeAsync(user.Id, cancellationToken))
        {
            return Result.Failure(
                Error.Forbidden("AUTH-2FA-LAST-METHOD-LOCK",
                    "System hesabınız için son 2FA yöntemini kaldıramazsınız."));
        }

        if (isEmail)
        {
            user.EmailConfirmed = false;
        }
        else
        {
            user.PhoneNumberConfirmed = false;
        }

        if (!hasOtherMethods)
        {
            await _userManager.SetTwoFactorEnabledAsync(user, false);
        }

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Failure(
                Error.Failure("AUTH-2FA-METHOD-REMOVE-FAILED", "Yöntem kaldırılamadı."));
        }

        return Result.Success();
    }
}
