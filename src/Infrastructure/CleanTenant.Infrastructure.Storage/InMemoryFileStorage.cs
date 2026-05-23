using System.Collections.Concurrent;
using CleanTenant.Application.Common.Storage;

namespace CleanTenant.Infrastructure.Storage;

/// <summary>
/// <see cref="IFileStorage"/>'ın bellek-içi (process-local) implementasyonu.
/// MinIO yapılandırılmadığında (örn. integration testleri, hızlı yerel deneme)
/// uygulamanın MinIO container'ı olmadan ayağa kalkabilmesi için kullanılır.
/// Production'da ASLA kayıt edilmez — orada gerçek MinIO zorunludur.
/// </summary>
public sealed class InMemoryFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, StoredFile> _store = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _store[key] = new StoredFile(buffer.ToArray(), contentType);
    }

    /// <inheritdoc />
    public Task<StoredFile?> GetAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(key, out var file) ? file : null);

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.ContainsKey(key));
}
