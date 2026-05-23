using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Storage;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Profile.GetPhoto;

/// <summary>Profil fotoğrafını object storage'dan okur.</summary>
public sealed class GetProfilePhotoQueryHandler
    : IRequestHandler<GetProfilePhotoQuery, Result<ProfilePhotoData?>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IFileStorage _fileStorage;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetProfilePhotoQueryHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor,
        IFileStorage fileStorage)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
        _fileStorage = fileStorage;
    }

    /// <summary>Sorguyu çalıştırır.</summary>
    public async Task<Result<ProfilePhotoData?>> Handle(
        GetProfilePhotoQuery query,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result<ProfilePhotoData?>.Failure(
                Error.NotFound("PROFILE-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        if (string.IsNullOrEmpty(user.ProfilePhotoKey))
        {
            return Result<ProfilePhotoData?>.Success(null);
        }

        var file = await _fileStorage.GetAsync(user.ProfilePhotoKey, cancellationToken);
        if (file is null)
        {
            return Result<ProfilePhotoData?>.Success(null);
        }

        return Result<ProfilePhotoData?>.Success(new ProfilePhotoData(file.Content, file.ContentType));
    }
}
