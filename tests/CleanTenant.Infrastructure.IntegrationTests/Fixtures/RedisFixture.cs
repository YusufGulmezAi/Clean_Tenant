using StackExchange.Redis;
using Testcontainers.Redis;

namespace CleanTenant.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// <para>
/// Test sınıfı seviyesinde paylaşılan Redis container'ı. v0.2.3.g —
/// HybridCacheStore / CacheInvalidationSubscriber / CachedAuthSessionStore
/// integration testleri için gerçek Redis bağlantısı sağlar.
/// </para>
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    /// <summary>Test'in kullanacağı StackExchange.Redis bağlantı dizgesi.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Paylaşılan multiplexer — testlerin tükettiği singleton.</summary>
    public IConnectionMultiplexer Multiplexer { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        Multiplexer = await ConnectionMultiplexer.ConnectAsync(ConnectionString + ",allowAdmin=true");
    }

    /// <summary>Yeni bir bağımsız multiplexer döner (multi-instance L1 senaryosu için).</summary>
    public Task<IConnectionMultiplexer> CreateNewMultiplexerAsync()
        => ConnectionMultiplexer.ConnectAsync(ConnectionString + ",allowAdmin=true").ContinueWith(t => (IConnectionMultiplexer)t.Result);

    /// <summary>Test sınıfları arası Redis'i temiz tutmak için FLUSHDB çağırır.</summary>
    public async Task FlushAsync()
    {
        foreach (var ep in Multiplexer.GetEndPoints())
        {
            var server = Multiplexer.GetServer(ep);
            await server.FlushDatabaseAsync();
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await Multiplexer.DisposeAsync();
        await _container.DisposeAsync();
    }
}
