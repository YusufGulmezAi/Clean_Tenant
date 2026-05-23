using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Profile.GetSummary;

/// <summary>Authenticated kullanıcının profil özeti (genel sekme + dil sekmesi için).</summary>
public sealed record GetProfileSummaryQuery : IRequest<Result<ProfileSummaryResult>>;

/// <summary>Profil özet bilgisi.</summary>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="Email">E-posta adresi (yoksa null).</param>
/// <param name="UserName">Kullanıcı adı.</param>
/// <param name="PreferredCulture">Kayıtlı dil tercihi (BCP-47, yoksa null).</param>
/// <param name="HasPhoto">Profil fotoğrafı var mı.</param>
/// <param name="PhotoUpdatedAt">Fotoğrafın son güncellenme zamanı (cache busting için).</param>
public sealed record ProfileSummaryResult(
    string FirstName,
    string LastName,
    string? Email,
    string? UserName,
    string? PreferredCulture,
    bool HasPhoto,
    DateTimeOffset? PhotoUpdatedAt);
