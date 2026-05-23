using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Storage;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Profile.UploadPhoto;

/// <summary>
/// Profil fotoğrafı yükleme akışı:
/// <list type="number">
///   <item>MIME tipi ve boyut <see cref="ProfilePhotoPolicy"/>'ye göre doğrulanır.</item>
///   <item>Görsel 100x100 kareye kırpılıp PNG'ye dönüştürülür (<see cref="IImageProcessor"/>).</item>
///   <item>Object storage'a kullanıcı anahtarıyla yazılır (öncekini üzerine yazar).</item>
///   <item><c>User.ProfilePhotoKey</c> + <c>ProfilePhotoUpdatedAt</c> güncellenir.</item>
/// </list>
/// </summary>
public sealed class UploadProfilePhotoCommandHandler
    : IRequestHandler<UploadProfilePhotoCommand, Result<UploadProfilePhotoResult>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IImageProcessor _imageProcessor;
    private readonly IFileStorage _fileStorage;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UploadProfilePhotoCommandHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor,
        IImageProcessor imageProcessor,
        IFileStorage fileStorage,
        IClock clock)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
        _imageProcessor = imageProcessor;
        _fileStorage = fileStorage;
        _clock = clock;
    }

    /// <summary>Yükleme isteğini işler.</summary>
    public async Task<Result<UploadProfilePhotoResult>> Handle(
        UploadProfilePhotoCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Length > ProfilePhotoPolicy.MaxUploadBytes)
        {
            return Result<UploadProfilePhotoResult>.Failure(
                Error.Validation("PROFILE-PHOTO-TOO-LARGE",
                    $"Dosya en fazla {ProfilePhotoPolicy.MaxUploadBytes / (1024 * 1024)} MB olabilir."));
        }

        if (!ProfilePhotoPolicy.AllowedContentTypes.Contains(command.ContentType))
        {
            return Result<UploadProfilePhotoResult>.Failure(
                Error.Validation("PROFILE-PHOTO-INVALID-TYPE",
                    "Yalnız JPEG, PNG veya WebP görseller yüklenebilir."));
        }

        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result<UploadProfilePhotoResult>.Failure(
                Error.NotFound("PROFILE-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        // Akışı seekable belleğe kopyala (boyut kapısıyla) — görsel kütüphanesi
        // ve storage seekable akış bekler.
        using var buffer = new MemoryStream();
        await command.Content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > ProfilePhotoPolicy.MaxUploadBytes)
        {
            return Result<UploadProfilePhotoResult>.Failure(
                Error.Validation("PROFILE-PHOTO-TOO-LARGE",
                    $"Dosya en fazla {ProfilePhotoPolicy.MaxUploadBytes / (1024 * 1024)} MB olabilir."));
        }
        buffer.Position = 0;

        ProcessedImage processed;
        try
        {
            processed = await _imageProcessor.ToSquarePngAsync(
                buffer, ProfilePhotoPolicy.Dimension, cancellationToken);
        }
        catch (InvalidImageException ex)
        {
            return Result<UploadProfilePhotoResult>.Failure(
                Error.Validation("PROFILE-PHOTO-INVALID-IMAGE", ex.Message));
        }

        var key = ProfilePhotoPolicy.KeyFor(user.Id);
        using (var pngStream = new MemoryStream(processed.Content))
        {
            await _fileStorage.UploadAsync(
                key, pngStream, ProfilePhotoPolicy.StoredContentType, cancellationToken);
        }

        var now = _clock.UtcNow;
        user.ProfilePhotoKey = key;
        user.ProfilePhotoUpdatedAt = now;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result<UploadProfilePhotoResult>.Failure(
                Error.Failure("PROFILE-PHOTO-SAVE-FAILED",
                    "Profil fotoğrafı kaydedilemedi."));
        }

        return Result<UploadProfilePhotoResult>.Success(new UploadProfilePhotoResult(now));
    }
}
