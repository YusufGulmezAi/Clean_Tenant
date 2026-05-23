using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.AspNetCore.Identity;

using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.GetTwoFactorMethods;

/// <summary>Kullanıcının 2FA durumunu özetler.</summary>
public sealed class GetTwoFactorMethodsQueryHandler : IRequestHandler<GetTwoFactorMethodsQuery, Result<GetTwoFactorMethodsResult>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly LoginFinalizer _finalizer;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetTwoFactorMethodsQueryHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor,
        LoginFinalizer finalizer)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
        _finalizer = finalizer;
    }

    /// <summary>Sorguyu çalıştırır.</summary>
    public async Task<Result<GetTwoFactorMethodsResult>> Handle(
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
        var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
        var isSystemScope = await _finalizer.HasSystemScopeAsync(user.Id, cancellationToken);

        return Result<GetTwoFactorMethodsResult>.Success(
            new GetTwoFactorMethodsResult(
                user.TwoFactorEnabled,
                [.. providers],
                recoveryLeft,
                AuthenticatorEnrolled: !string.IsNullOrWhiteSpace(authenticatorKey),
                EmailConfirmed: user.EmailConfirmed,
                PhoneConfirmed: user.PhoneNumberConfirmed,
                Email: user.Email,
                PhoneNumber: user.PhoneNumber,
                IsSystemScope: isSystemScope));
    }
}
