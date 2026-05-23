using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Application.Features.Profile.GetSummary;

/// <summary>Profil özetini döner.</summary>
public sealed class GetProfileSummaryQueryHandler : IRequestHandler<GetProfileSummaryQuery, Result<ProfileSummaryResult>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetProfileSummaryQueryHandler(
        UserManager<User> userManager,
        ICurrentSessionAccessor sessionAccessor)
    {
        _userManager = userManager;
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Sorguyu çalıştırır.</summary>
    public async Task<Result<ProfileSummaryResult>> Handle(
        GetProfileSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            return Result<ProfileSummaryResult>.Failure(
                Error.NotFound("PROFILE-USER-NOT-FOUND", "Kullanıcı bulunamadı."));
        }

        return Result<ProfileSummaryResult>.Success(new ProfileSummaryResult(
            user.FirstName,
            user.LastName,
            user.Email,
            user.UserName,
            user.PreferredCulture,
            HasPhoto: !string.IsNullOrEmpty(user.ProfilePhotoKey),
            PhotoUpdatedAt: user.ProfilePhotoUpdatedAt));
    }
}
