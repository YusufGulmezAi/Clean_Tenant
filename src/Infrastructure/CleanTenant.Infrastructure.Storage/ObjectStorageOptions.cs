namespace CleanTenant.Infrastructure.Storage;

/// <summary>
/// MinIO / S3 bağlantı ve bucket ayarları. <c>ObjectStorage</c> konfigürasyon
/// bölümünden bağlanır (appsettings + ortam başına <c>.env</c>; Production'da
/// erişim anahtarları vault'tan gelir).
/// </summary>
public sealed class ObjectStorageOptions
{
    /// <summary>Konfigürasyon bölüm adı.</summary>
    public const string SectionName = "ObjectStorage";

    /// <summary>Sunucu adresi, şema olmadan (örn. <c>localhost:9000</c>).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Erişim anahtarı (S3 access key).</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Gizli anahtar (S3 secret key).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>TLS kullanılsın mı (Production'da true).</summary>
    public bool UseSsl { get; set; }

    /// <summary>Dosyaların yazılacağı bucket adı.</summary>
    public string Bucket { get; set; } = "cleantenant-files";

    /// <summary>
    /// Startup'ta bucket yoksa otomatik oluşturulsun mu. Dev/Test'te <c>true</c>;
    /// Production'da bucket altyapı tarafından önceden hazırlandığından <c>false</c>
    /// bırakılabilir.
    /// </summary>
    public bool CreateBucketIfMissing { get; set; } = true;
}
