namespace CleanTenant.Application.Common.Storage;

/// <summary>
/// Yüklenen görselleri sunucu tarafında işleyen sözleşme. v0.2.13'te profil
/// fotoğrafını sabit kare boyuta (100x100) getirmek için devreye girdi.
/// Implementasyon (ImageSharp) Infrastructure katmanındadır; Application katmanı
/// görsel kütüphanesine doğrudan bağımlı olmaz (Clean Architecture).
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Kaynağı merkezden kare kırpıp <paramref name="size"/> x <paramref name="size"/>
    /// boyutunda PNG'ye dönüştürür. Geçersiz/bozuk görselde
    /// <see cref="InvalidImageException"/> fırlatır.
    /// </summary>
    /// <param name="source">Yüklenen görselin akışı.</param>
    /// <param name="size">Hedef kenar uzunluğu (piksel).</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    Task<ProcessedImage> ToSquarePngAsync(Stream source, int size, CancellationToken cancellationToken = default);
}

/// <summary>İşlenmiş görselin PNG içeriği ve boyut bilgisi.</summary>
/// <param name="Content">PNG ikili içeriği.</param>
/// <param name="Width">Genişlik (piksel).</param>
/// <param name="Height">Yükseklik (piksel).</param>
public sealed record ProcessedImage(byte[] Content, int Width, int Height);

/// <summary>Görsel çözümlenemediğinde (bozuk / desteklenmeyen format) fırlatılır.</summary>
public sealed class InvalidImageException : Exception
{
    /// <summary>Varsayılan mesajla oluşturur.</summary>
    public InvalidImageException(string message) : base(message) { }

    /// <summary>İç istisnayla oluşturur.</summary>
    public InvalidImageException(string message, Exception inner) : base(message, inner) { }
}
