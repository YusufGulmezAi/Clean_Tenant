using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.PasswordChange;

/// <summary>
/// <see cref="CompletePasswordChangeCommand"/> handler.
/// Challenge'ı doğrular, şifreyi değiştirir, RequiresPasswordChange = false set eder,
/// normal login akışını (LoginFinalizer) tamamlar.
/// </summary>
public sealed class CompletePasswordChangeCommandHandler : IRequestHandler<CompletePasswordChangeCommand, Result<TokenPair>>
{
    private readonly IPasswordChangeChallengeStore _store;
    private readonly UserManager<User> _userManager;
    private readonly LoginFinalizer _finalizer;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CompletePasswordChangeCommandHandler(
        IPasswordChangeChallengeStore store,
        UserManager<User> userManager,
        LoginFinalizer finalizer)
    {
        _store = store;
        _userManager = userManager;
        _finalizer = finalizer;
    }

    /// <inheritdoc />
    public async Task<Result<TokenPair>> Handle(
        CompletePasswordChangeCommand command,
        CancellationToken cancellationToken)
    {
        var challenge = await _store.GetAsync(command.ChallengeToken, cancellationToken);
        if (challenge is null)
        {
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-020", "Şifre değişim oturumu geçersiz veya süresi dolmuş."));
        }

        var user = await _userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            await _store.DeleteAsync(command.ChallengeToken, cancellationToken);
            return Result<TokenPair>.Failure(
                Error.NotFound("AUTH-021", "Kullanıcı bulunamadı veya devre dışı."));
        }

        // Yeni şifreyi set et
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, token, command.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
            return Result<TokenPair>.Failure(
                Error.Validation("AUTH-022", $"Şifre değiştirilemedi: {errors}"));
        }

        // Şifre değişimi zorunluluğunu kaldır
        user.RequiresPasswordChange = false;
        await _userManager.UpdateAsync(user);

        // Challenge'ı sil (replay engelleme)
        await _store.DeleteAsync(command.ChallengeToken, cancellationToken);

        // Normal login finalize
        return await _finalizer.FinalizeAsync(
            user,
            challenge.Persona,
            challenge.ContextId,
            command.IpAddress,
            command.UserAgent,
            cancellationToken);
    }
}
