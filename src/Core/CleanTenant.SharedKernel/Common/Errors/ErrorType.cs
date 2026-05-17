namespace CleanTenant.SharedKernel.Common.Errors;

/// <summary>
/// İş hatalarının (Error) kategorisini belirtir. Result pattern'i içinde
/// taşınan hataların türünü standartlaştırır; üst katmanlar (örn. API
/// envelope ya da global exception middleware) bu türe bakarak uygun
/// HTTP status code'a eşler:
/// <list type="bullet">
///   <item><see cref="Validation"/>     → 400 Bad Request</item>
///   <item><see cref="NotFound"/>       → 404 Not Found</item>
///   <item><see cref="Conflict"/>       → 409 Conflict</item>
///   <item><see cref="Unauthorized"/>   → 401 Unauthorized</item>
///   <item><see cref="Forbidden"/>      → 403 Forbidden</item>
///   <item><see cref="Failure"/>        → 422 Unprocessable Entity (genel iş kuralı ihlali)</item>
///   <item><see cref="Critical"/>       → 500 Internal Server Error (beklenmedik)</item>
/// </list>
/// </summary>
public enum ErrorType
{
    /// <summary>Hata yok (Error.None için kullanılır).</summary>
    None = 0,

    /// <summary>Girdi doğrulama hatası (FluentValidation ya da el ile).</summary>
    Validation = 1,

    /// <summary>Aranan kaynak bulunamadı.</summary>
    NotFound = 2,

    /// <summary>Tekrarlı kayıt / iş kuralı çakışması / concurrency conflict.</summary>
    Conflict = 3,

    /// <summary>Yetkisiz: oturum yok ya da geçersiz.</summary>
    Unauthorized = 4,

    /// <summary>Yasak: oturum var ama bu işleme yetki yok.</summary>
    Forbidden = 5,

    /// <summary>Genel iş kuralı ihlali (validation harici).</summary>
    Failure = 6,

    /// <summary>Beklenmedik / sistemik hata.</summary>
    Critical = 7,
}
