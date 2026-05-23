using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Profile.UploadPhoto;

/// <summary>
/// Authenticated kullanıcının kendi profil fotoğrafını yükler. Görsel sunucu
/// tarafında 100x100 kareye kırpılıp PNG'ye dönüştürülür ve object storage'a
/// (MinIO) yazılır. Kim olduğu oturumdan okunur.
/// </summary>
/// <param name="Content">Yüklenen görselin akışı (jpeg/png/webp).</param>
/// <param name="ContentType">Yüklenen dosyanın MIME tipi.</param>
/// <param name="Length">Dosya boyutu (byte); limit kontrolü için.</param>
public sealed record UploadProfilePhotoCommand(Stream Content, string ContentType, long Length)
    : IRequest<Result<UploadProfilePhotoResult>>;

/// <summary>
/// Yükleme yanıtı. <see cref="UpdatedAt"/> önbellek kırma (cache busting) için
/// UI tarafından img URL'ine sorgu parametresi olarak eklenir.
/// </summary>
/// <param name="UpdatedAt">Fotoğrafın güncellenme zamanı (UTC).</param>
public sealed record UploadProfilePhotoResult(DateTimeOffset UpdatedAt);
