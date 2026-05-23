using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Profile.RemovePhoto;

/// <summary>
/// Authenticated kullanıcının profil fotoğrafını kaldırır: object storage'daki
/// nesneyi siler ve <c>User.ProfilePhotoKey</c>'i temizler. Fotoğraf yoksa
/// işlem idempotent biçimde başarılı sayılır.
/// </summary>
public sealed record RemoveProfilePhotoCommand : IRequest<Result>;
