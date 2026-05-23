using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Profile.SetTheme;

/// <summary>Tema tercihini kalıcılaştırır.</summary>
public sealed class SetUserThemeCommandHandler : IRequestHandler<SetUserThemeCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SetUserThemeCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Komutu işler.</summary>
    public async Task<Result> Handle(SetUserThemeCommand command, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("PROFILE-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        user.PreferredThemePreset = command.Preset;
        user.PreferredDarkMode = command.DarkMode;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Failure(
                Error.Failure("PROFILE-THEME-SAVE-FAILED", "Tema tercihi kaydedilemedi."));
        }

        return Result.Success();
    }
}
