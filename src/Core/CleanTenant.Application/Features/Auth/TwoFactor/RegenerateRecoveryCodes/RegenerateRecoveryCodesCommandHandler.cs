using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using Microsoft.AspNetCore.Identity;

using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.RegenerateRecoveryCodes;

/// <summary>
/// 10 yeni recovery code üretir; eski kodları invalidate eder.
/// Kullanıcı 2FA aktif olmadan recovery üretemez (anlam taşımaz).
/// </summary>
public sealed class RegenerateRecoveryCodesCommandHandler : IRequestHandler<RegenerateRecoveryCodesCommand, Result<RegenerateRecoveryCodesResult>>
{
    private const int RecoveryCodeCount = 10;

    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RegenerateRecoveryCodesCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>İsteği işler.</summary>
    public async Task<Result<RegenerateRecoveryCodesResult>> Handle(
        RegenerateRecoveryCodesCommand command,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result<RegenerateRecoveryCodesResult>.Failure(
                Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        if (!user.TwoFactorEnabled)
        {
            return Result<RegenerateRecoveryCodesResult>.Failure(
                Error.Failure("AUTH-2FA-NOT-ENABLED", "Recovery code üretmek için 2FA aktif olmalı."));
        }

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);
        return Result<RegenerateRecoveryCodesResult>.Success(
            new RegenerateRecoveryCodesResult(codes?.ToList() ?? []));
    }
}
