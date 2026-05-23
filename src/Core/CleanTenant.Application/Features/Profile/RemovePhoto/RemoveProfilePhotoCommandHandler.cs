using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Storage;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Profile.RemovePhoto;

/// <summary>Profil fotoğrafı kaldırma akışını işler.</summary>
public sealed class RemoveProfilePhotoCommandHandler : IRequestHandler<RemoveProfilePhotoCommand, Result>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IFileStorage _fileStorage;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveProfilePhotoCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor,
        IFileStorage fileStorage)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
        _fileStorage = fileStorage;
    }

    /// <summary>Kaldırma isteğini işler.</summary>
    public async Task<Result> Handle(RemoveProfilePhotoCommand command, CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("PROFILE-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        if (string.IsNullOrEmpty(user.ProfilePhotoKey))
        {
            return Result.Success(); // Zaten fotoğraf yok — idempotent.
        }

        await _fileStorage.DeleteAsync(user.ProfilePhotoKey, cancellationToken);

        user.ProfilePhotoKey = null;
        user.ProfilePhotoUpdatedAt = null;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Failure(
                Error.Failure("PROFILE-PHOTO-SAVE-FAILED", "Profil fotoğrafı kaldırılamadı."));
        }

        return Result.Success();
    }
}
