using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Profile.GetPhoto;

/// <summary>
/// Authenticated kullanıcının profil fotoğrafını object storage'dan getirir.
/// Fotoğraf yoksa <c>null</c> içerik döner (UI baş harf avatar'ına düşer).
/// </summary>
public sealed record GetProfilePhotoQuery : IRequest<Result<ProfilePhotoData?>>;

/// <summary>İndirilen profil fotoğrafının ikili içeriği ve MIME tipi.</summary>
/// <param name="Content">PNG ikili içeriği.</param>
/// <param name="ContentType">MIME tipi (her zaman <c>image/png</c>).</param>
public sealed record ProfilePhotoData(byte[] Content, string ContentType);
