using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CleanTenant.Infrastructure.Storage;

/// <summary>
/// Startup'ta hedef bucket'ı (yoksa ve <see cref="ObjectStorageOptions.CreateBucketIfMissing"/>
/// true ise) oluşturur. Best-effort: MinIO henüz ayağa kalkmamışsa uygulamayı
/// çökertmez, yalnız uyarı loglar — ilk yüklemede bucket gerçek erişimde de oluşur.
/// </summary>
public sealed class MinioBucketInitializer : IHostedService
{
    private readonly IMinioClient _client;
    private readonly ObjectStorageOptions _options;
    private readonly ILogger<MinioBucketInitializer> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public MinioBucketInitializer(
        IMinioClient client,
        IOptions<ObjectStorageOptions> options,
        ILogger<MinioBucketInitializer> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.CreateBucketIfMissing)
        {
            return;
        }

        try
        {
            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.Bucket), cancellationToken);

            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_options.Bucket), cancellationToken);
                _logger.LogInformation("Object storage bucket '{Bucket}' oluşturuldu.", _options.Bucket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Object storage bucket '{Bucket}' başlangıçta hazırlanamadı (MinIO erişilemiyor olabilir).",
                _options.Bucket);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
