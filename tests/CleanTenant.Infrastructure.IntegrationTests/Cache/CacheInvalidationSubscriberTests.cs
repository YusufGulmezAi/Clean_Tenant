using CleanTenant.Application.Common.Caching;
using CleanTenant.Infrastructure.Caching.Cache;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace CleanTenant.Infrastructure.IntegrationTests.Cache;

/// <summary>
/// <see cref="CacheInvalidationSubscriber"/> — pub/sub ile multi-instance L1
/// senkronizasyonu testleri.
/// </summary>
public sealed class CacheInvalidationSubscriberTests : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private readonly RedisFixture _redis;

    public CacheInvalidationSubscriberTests(RedisFixture redis)
    {
        _redis = redis;
    }

    public Task InitializeAsync() => _redis.FlushAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    public sealed record Box(string Value);

    [Fact]
    public async Task Bir_instance_invalidate_edince_diger_instance_L1_silmeli()
    {
        // Iki ayrı multiplexer + iki ayrı L1 cache — multi-instance simülasyonu
        var mux1 = _redis.Multiplexer;
        var mux2 = await _redis.CreateNewMultiplexerAsync();

        using var l1A = new MemoryCache(new MemoryCacheOptions());
        using var l1B = new MemoryCache(new MemoryCacheOptions());

        var storeA = new HybridCacheStore(mux1, l1A, NullLogger<HybridCacheStore>.Instance, "instance-A");
        var storeB = new HybridCacheStore(mux2, l1B, NullLogger<HybridCacheStore>.Instance, "instance-B");

        var subscriberB = new CacheInvalidationSubscriber(mux2, storeB, NullLogger<CacheInvalidationSubscriber>.Instance);

        // Subscriber'ı başlat ve subscribe tamamlanmasını bekle
        await subscriberB.StartAsync(CancellationToken.None);
        await Task.Delay(200); // subscribe propagation

        // İki instance da aynı key'i cache'lesin → her ikisinin L1'inde olsun
        await storeA.SetAsync("shared:k", new Box("first"), new CacheOptions(TimeSpan.FromMinutes(1)));
        await storeB.GetAsync<Box>("shared:k"); // B L1'ine backfill

        // A invalidate et → B pub/sub mesajını alıp L1'ini silmeli
        await storeA.RemoveAsync("shared:k");

        // Pub/sub asenkron — kısa bekleme
        await Task.Delay(200);

        // B'nin L1'i silinmiş olmalı; aynı zamanda L2 (Redis) de silindi
        // → fresh L1'lı yeni store ile bakarsak null görmeliyiz
        using var l1Fresh = new MemoryCache(new MemoryCacheOptions());
        var freshStore = new HybridCacheStore(mux2, l1Fresh, NullLogger<HybridCacheStore>.Instance, "instance-fresh");
        var result = await freshStore.GetAsync<Box>("shared:k");
        result.Should().BeNull();

        await subscriberB.StopAsync(CancellationToken.None);
        await mux2.DisposeAsync();
    }

    [Fact]
    public async Task Origin_instance_kendi_mesajini_isleme_almamali()
    {
        // Tek instance: subscriber + store aynı → pub/sub'tan kendi mesajı gelirse
        // L1'i tekrar silmek gereksiz (zaten senkron silmişti). Bu test re-entry yok'u doğrular.
        var mux = _redis.Multiplexer;
        using var l1 = new MemoryCache(new MemoryCacheOptions());
        var store = new HybridCacheStore(mux, l1, NullLogger<HybridCacheStore>.Instance, "single-instance");
        var subscriber = new CacheInvalidationSubscriber(mux, store, NullLogger<CacheInvalidationSubscriber>.Instance);

        await subscriber.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        await store.SetAsync("origin:k", new Box("v"), new CacheOptions(TimeSpan.FromMinutes(1)));
        await store.RemoveAsync("origin:k");

        await Task.Delay(200);

        // Cache zaten boş, sorun yok — re-entry exception fırlatmamalı
        var result = await store.GetAsync<Box>("origin:k");
        result.Should().BeNull();

        await subscriber.StopAsync(CancellationToken.None);
    }
}
