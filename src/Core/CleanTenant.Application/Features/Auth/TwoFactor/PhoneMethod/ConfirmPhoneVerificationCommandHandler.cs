using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PhoneMethod;

/// <summary>
/// SMS kodunu doğrular ve telefonu hesaba kaydeder. <see cref="UserManager{TUser}.ChangePhoneNumberAsync"/>
/// token doğrulanırsa numarayı yazar ve <c>PhoneNumberConfirmed=true</c> yapar.
/// </summary>
public sealed class ConfirmPhoneVerificationCommandHandler
    : IRequestHandler<ConfirmPhoneVerificationCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ConfirmPhoneVerificationCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>İsteği işler.</summary>
    public async Task<Result> Handle(ConfirmPhoneVerificationCommand command, CancellationToken cancellationToken)
    {
        if (!LoginIdentifier.TryNormalizePhone(command.Phone, out var phone))
        {
            return Result.Failure(
                Error.Validation("AUTH-2FA-PHONE-INVALID", "Geçerli bir Türkiye cep telefonu girin."));
        }

        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        var change = await _userManager.ChangePhoneNumberAsync(user, phone, command.Code.Trim());
        if (!change.Succeeded)
        {
            return Result.Failure(
                Error.Unauthorized("AUTH-2FA-INVALID-CODE", "Kod hatalı veya süresi dolmuş."));
        }

        return Result.Success();
    }
}
