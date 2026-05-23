namespace CleanTenant.Application.Common.Storage;

/// <summary>
/// S3-uyumlu object storage (MinIO) için genel dosya saklama sözleşmesi.
/// v0.2.13'te profil fotoğrafı için devreye girdi; ileride fatura PDF'leri,
/// ekler gibi ikili içerikler de aynı interface üzerinden saklanır.
/// </summary>
/// <remarks>
/// Anahtar (<c>key</c>) bucket içindeki nesne yolu olarak kullanılır
/// (örn. <c>avatars/3f1c...png</c>). Bucket adı implementasyon tarafında
/// konfigürasyondan okunur; çağıran yalnız anahtarı verir.
/// </remarks>
public interface IFileStorage
{
    /// <summary>Bir nesneyi yükler; aynı anahtar varsa üzerine yazar.</summary>
    /// <param name="key">Bucket içi nesne anahtarı.</param>
    /// <param name="content">İçerik akışı (baştan okunabilir olmalı).</param>
    /// <param name="contentType">MIME tipi (örn. <c>image/png</c>).</param>
    /// <param name="cancellationToken">İptal token'ı.</param>
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Nesneyi indirir; yoksa <c>null</c> döner.</summary>
    Task<StoredFile?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Nesneyi siler; yoksa sessizce başarılı sayılır (idempotent).</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Nesnenin var olup olmadığını kontrol eder.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>Object storage'dan indirilen bir nesnenin içerik + MIME bilgisi.</summary>
/// <param name="Content">Nesnenin ikili içeriği.</param>
/// <param name="ContentType">MIME tipi.</param>
public sealed record StoredFile(byte[] Content, string ContentType);
