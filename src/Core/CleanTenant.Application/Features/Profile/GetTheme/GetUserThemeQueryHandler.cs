using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Profile.GetTheme;

/// <summary>
/// Tema tercihini döner. Oturum yoksa veya kullanıcı bulunamazsa varsayılan
/// (preset null → "Kurumsal Mavi", açık mod) döner; bu yüzden anonim/erken
/// bağlamda da güvenle çağrılabilir (throw etmez).
/// </summary>
public sealed class GetUserThemeQueryHandler : IRequestHandler<GetUserThemeQuery, Result<UserThemeResult>>
{
    private static readonly UserThemeResult Default = new(null, false);

    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetUserThemeQueryHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Sorguyu çalıştırır.</summary>
    public async Task<Result<UserThemeResult>> Handle(
        GetUserThemeQuery query,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current;
        if (session is null)
        {
            return Result<UserThemeResult>.Success(Default);
        }

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result<UserThemeResult>.Success(Default);
        }

        return Result<UserThemeResult>.Success(
            new UserThemeResult(user.PreferredThemePreset, user.PreferredDarkMode));
    }
}
