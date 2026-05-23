using CleanTenant.Application.Common.Storage;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace CleanTenant.Infrastructure.Storage;

/// <summary>
/// <see cref="IFileStorage"/>'ın MinIO (S3-uyumlu) implementasyonu. Bucket adı
/// <see cref="ObjectStorageOptions"/>'tan okunur; çağıran yalnız nesne anahtarını
/// verir.
/// </summary>
public sealed class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly ObjectStorageOptions _options;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public MinioFileStorage(IMinioClient client, IOptions<ObjectStorageOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        // Minio PutObject akış boyutunu ister; seekable değilse belleğe alıp boyutu hesapla.
        Stream uploadStream = content;
        long size;
        if (content.CanSeek)
        {
            size = content.Length - content.Position;
        }
        else
        {
            var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            uploadStream = buffer;
            size = buffer.Length;
        }

        try
        {
            var args = new PutObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(key)
                .WithStreamData(uploadStream)
                .WithObjectSize(size)
                .WithContentType(contentType);

            await _client.PutObjectAsync(args, cancellationToken);
        }
        finally
        {
            if (!ReferenceEquals(uploadStream, content))
            {
                await uploadStream.DisposeAsync();
            }
        }
    }

    /// <inheritdoc />
    public async Task<StoredFile?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            using var buffer = new MemoryStream();
            var args = new GetObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(key)
                .WithCallbackStream((stream, ct) => stream.CopyToAsync(buffer, ct));

            var stat = await _client.GetObjectAsync(args, cancellationToken);
            var contentType = string.IsNullOrWhiteSpace(stat.ContentType)
                ? "application/octet-stream"
                : stat.ContentType;

            return new StoredFile(buffer.ToArray(), contentType);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (BucketNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(key);

            await _client.RemoveObjectAsync(args, cancellationToken);
        }
        catch (ObjectNotFoundException)
        {
            // Idempotent: zaten yoksa başarı say.
        }
        catch (BucketNotFoundException)
        {
            // Bucket hiç yoksa silinecek nesne de yok.
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(key);

            await _client.StatObjectAsync(args, cancellationToken);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (BucketNotFoundException)
        {
            return false;
        }
    }
}
