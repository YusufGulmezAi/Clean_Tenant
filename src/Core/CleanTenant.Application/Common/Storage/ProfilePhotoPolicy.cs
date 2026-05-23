namespace CleanTenant.Application.Common.Storage;

/// <summary>
/// Profil fotoğrafı kuralları (boyut, dosya limiti, izinli tipler, anahtar
/// üretimi) için tek doğruluk kaynağı. UI doğrulaması, command handler ve
/// testler aynı sabitleri kullanır.
/// </summary>
public static class ProfilePhotoPolicy
{
    /// <summary>Saklanan/serve edilen kare fotoğrafın kenar uzunluğu (piksel).</summary>
    public const int Dimension = 100;

    /// <summary>Yüklenebilecek en büyük dosya boyutu (4 MB).</summary>
    public const long MaxUploadBytes = 4L * 1024 * 1024;

    /// <summary>Object storage'da saklanan fotoğrafın MIME tipi (her zaman PNG).</summary>
    public const string StoredContentType = "image/png";

    /// <summary>Yükleme için kabul edilen kaynak MIME tipleri.</summary>
    public static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
        };

    /// <summary>
    /// Bir kullanıcının avatar nesne anahtarını üretir. Kullanıcı başına tek
    /// anahtar; yeni yükleme öncekini üzerine yazar (storage'da artık birikmez).
    /// </summary>
    public static string KeyFor(Guid userId) => $"avatars/{userId:N}.png";
}
