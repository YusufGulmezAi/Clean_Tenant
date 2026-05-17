using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PreAuthEnrollment;

/// <summary>
/// Pre-auth enrollment'ı sonlandırır:
/// <list type="number">
///   <item>Challenge'ı Redis'ten oku — yoksa hata.</item>
///   <item><see cref="PreAuthEnrollmentChallenge.VerifiedAt"/> set olmalı; aksi takdirde
///   Complete adımı geçilmedi → reddet (atlatma engeli).</item>
///   <item>Kullanıcıyı yükle, son kez 2FA'nın gerçekten aktif olduğunu doğrula.</item>
///   <item><see cref="LoginFinalizer"/> ile <c>TokenPair</c> üret (challenge'taki Persona +
///   ContextId + IpAddress/UserAgent kullanılır).</item>
///   <item>Challenge'ı sil (replay engelleme).</item>
/// </list>
/// </summary>
public sealed class FinalizePreAuthEnrollmentCommandHandler
    : IRequestHandler<FinalizePreAuthEnrollmentCommand, Result<TokenPair>>
{
    private readonly IPreAuthEnrollmentStore _store;
    private readonly UserManager<User> _userManager;
    private readonly LoginFinalizer _finalizer;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public FinalizePreAuthEnrollmentCommandHandler(
        IPreAuthEnrollmentStore store,
        UserManager<User> userManager,
        LoginFinalizer finalizer)
    {
        _store = store;
        _userManager = userManager;
        _finalizer = finalizer;
    }

    /// <summary>Finalize isteğini işler.</summary>
    public async Task<Result<TokenPair>> Handle(
        FinalizePreAuthEnrollmentCommand command,
        CancellationToken cancellationToken)
    {
        var challenge = await _store.GetAsync(command.ChallengeToken, cancellationToken);
        if (challenge is null)
        {
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-2FA-ENROLL-CHALLENGE-NOT-FOUND",
                    "Enrollment süresi doldu veya geçersiz token. Lütfen tekrar giriş yapın."));
        }

        if (challenge.VerifiedAt is null)
        {
            return Result<TokenPair>.Failure(
                Error.Forbidden("AUTH-2FA-ENROLL-NOT-VERIFIED",
                    "Enrollment doğrulanmadı — önce authenticator kodunu girin."));
        }

        var user = await _userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user is null)
        {
            await _store.DeleteAsync(command.ChallengeToken, cancellationToken);
            return Result<TokenPair>.Failure(
                Error.Unauthorized("AUTH-2FA-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        if (!user.TwoFactorEnabled)
        {
            // Çok düşük olasılıklı tutarsızlık — Complete adımı set ettiği halde
            // kullanıcı entity'sinde değişmemiş. Defansif kontrol.
            return Result<TokenPair>.Failure(
                Error.Failure("AUTH-2FA-NOT-ACTIVATED",
                    "2FA aktivasyonu tamamlanmadı — enrollment'ı baştan başlatın."));
        }

        var finalize = await _finalizer.FinalizeAsync(
            user,
            challenge.Persona,
            challenge.ContextId,
            command.IpAddress,
            command.UserAgent,
            cancellationToken);

        if (finalize.IsFailure)
        {
            return finalize;
        }

        await _store.DeleteAsync(command.ChallengeToken, cancellationToken);
        return finalize;
    }
}
