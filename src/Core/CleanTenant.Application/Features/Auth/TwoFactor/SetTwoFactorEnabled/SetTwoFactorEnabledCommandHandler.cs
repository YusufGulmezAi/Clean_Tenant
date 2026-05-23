using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.SetTwoFactorEnabled;

/// <summary>
/// 2FA ana anahtarını işler:
/// <list type="bullet">
///   <item><b>Aç:</b> En az bir doğrulanmış yöntem yoksa <c>AUTH-2FA-NO-METHOD</c>.</item>
///   <item><b>Kapat (System):</b> <c>AUTH-2FA-MANDATORY</c> (zorunlu).</item>
///   <item><b>Kapat (System dışı):</b> hesap şifresi doğrulanır
///         (<c>AUTH-2FA-PASSWORD-REQUIRED</c> / <c>AUTH-2FA-PASSWORD-INVALID</c>).</item>
/// </list>
/// </summary>
public sealed class SetTwoFactorEnabledCommandHandler : IRequestHandler<SetTwoFactorEnabledCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly LoginFinalizer _finalizer;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SetTwoFactorEnabledCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor,
        LoginFinalizer finalizer)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
        _finalizer = finalizer;
    }

    /// <summary>İsteği işler.</summary>
    public async Task<Result> Handle(SetTwoFactorEnabledCommand command, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        if (command.Enabled)
        {
            if (user.TwoFactorEnabled)
            {
                return Result.Success(); // Zaten aktif — idempotent.
            }

            var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);
            if (providers.Count == 0)
            {
                return Result.Failure(
                    Error.Validation("AUTH-2FA-NO-METHOD",
                        "2FA'yı etkinleştirmek için önce en az bir doğrulanmış yöntem ekleyin."));
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            return Result.Success();
        }

        // Kapatma talebi
        if (!user.TwoFactorEnabled)
        {
            return Result.Success(); // Zaten pasif — idempotent.
        }

        if (await _finalizer.HasSystemScopeAsync(user.Id, cancellationToken))
        {
            return Result.Failure(
                Error.Forbidden("AUTH-2FA-MANDATORY",
                    "System hesabınız için 2FA zorunludur; kapatılamaz."));
        }

        // System dışı kullanıcı 2FA'yı pasife alıyor → hesap şifresini doğrula.
        if (string.IsNullOrEmpty(command.Password))
        {
            return Result.Failure(
                Error.Validation("AUTH-2FA-PASSWORD-REQUIRED",
                    "2FA'yı kapatmak için hesap şifrenizi girin."));
        }

        if (!await _userManager.CheckPasswordAsync(user, command.Password))
        {
            return Result.Failure(
                Error.Unauthorized("AUTH-2FA-PASSWORD-INVALID", "Şifre hatalı."));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        return Result.Success();
    }
}
