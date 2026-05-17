namespace CleanTenant.SharedKernel.Common.Errors;

/// <summary>
/// <para>
/// Bir iş hatasının yapılandırılmış temsili. <see cref="Code"/> stabil
/// (loglama, lokalizasyon ve istemci-yönlendirme için), <see cref="Message"/>
/// kullanıcıya yönelik (lokalize edilebilir), <see cref="Type"/> kategori
/// (HTTP eşlemesi için).
/// </para>
/// <para>
/// <b>Kullanım:</b> Doğrudan ctor çağırmak yerine factory metotları tercih
/// edilir (<see cref="Validation"/>, <see cref="NotFound"/>, vb.) — kategori
/// ile kod tutarlılığı garanti altında.
/// </para>
/// </summary>
/// <param name="Code">Stabil hata kodu (örn. <c>"USR-001"</c>); modül + sıra
/// formatı. Lokalizasyon sözlüğünün anahtarı bu koddur.</param>
/// <param name="Message">Varsayılan (genelde İngilizce ya da TR fallback)
/// hata mesajı. Asıl kullanıcı mesajı response zarfında lokalize edilir.</param>
/// <param name="Type">Hata kategorisi; HTTP status code eşlemesi için.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>
    /// "Hata yok" durumu. Result.Success durumlarında FirstError için kullanılır.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>Doğrulama hatası üretir.</summary>
    public static Error Validation(string code, string message)
        => new(code, message, ErrorType.Validation);

    /// <summary>Bulunamadı hatası üretir.</summary>
    public static Error NotFound(string code, string message)
        => new(code, message, ErrorType.NotFound);

    /// <summary>Çakışma hatası üretir (tekrar kayıt, concurrency conflict).</summary>
    public static Error Conflict(string code, string message)
        => new(code, message, ErrorType.Conflict);

    /// <summary>Yetkisiz hatası üretir (oturum yok / token geçersiz).</summary>
    public static Error Unauthorized(string code, string message)
        => new(code, message, ErrorType.Unauthorized);

    /// <summary>Yasak hatası üretir (oturum var, izin yok).</summary>
    public static Error Forbidden(string code, string message)
        => new(code, message, ErrorType.Forbidden);

    /// <summary>Genel iş kuralı ihlali hatası üretir.</summary>
    public static Error Failure(string code, string message)
        => new(code, message, ErrorType.Failure);

    /// <summary>Kritik / beklenmedik hata üretir (genelde global exception middleware tarafından).</summary>
    public static Error Critical(string code, string message)
        => new(code, message, ErrorType.Critical);
}
