using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.GetTwoFactorMethods;

/// <summary>Kullanıcının 2FA durumunu özetler.</summary>
public sealed class GetTwoFactorMethodsQueryHandler
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetTwoFactorMethodsQueryHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Sorguyu çalıştırır.</summary>
    public async Task<Result<GetTwoFactorMethodsResult>> HandleAsync(
        GetTwoFactorMethodsQuery query,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result<GetTwoFactorMethodsResult>.Failure(
                Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);
        var recoveryLeft = await _userManager.CountRecoveryCodesAsync(user);

        return Result<GetTwoFactorMethodsResult>.Success(
            new GetTwoFactorMethodsResult(user.TwoFactorEnabled, [.. providers], recoveryLeft));
    }
}
