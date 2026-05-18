using CleanTenant.Application.Common.Caching;
using CleanTenant.Infrastructure.Caching.Cache;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace CleanTenant.Infrastructure.IntegrationTests.Cache;

/// <summary>
/// <see cref="HybridCacheStore"/> integration testleri — gerçek Redis (L2) ile
/// in-process IMemoryCache (L1) etkileşimi.
/// </summary>
public sealed class HybridCacheStoreTests : IClassFixture<RedisFixture>, IAsyncLifetime, IDisposable
{
    private readonly RedisFixture _redis;
    private HybridCacheStore _cache = null!;
    private MemoryCache _l1 = null!;

    public HybridCacheStoreTests(RedisFixture redis)
    {
        _redis = redis;
    }

    public async Task InitializeAsync()
    {
        await _redis.FlushAsync();
        _l1 = new MemoryCache(new MemoryCacheOptions());
        _cache = new HybridCacheStore(_redis.Multiplexer, _l1, NullLogger<HybridCacheStore>.Instance, "test-instance-1");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _l1?.Dispose();

    public sealed record TestDto(string Name, int Count);

    [Fact]
    public async Task Set_ve_Get_basit_okuma_yazma()
    {
        await _cache.SetAsync("k1", new TestDto("alpha", 7), new CacheOptions(TimeSpan.FromMinutes(1)));

        var result = await _cache.GetAsync<TestDto>("k1");

        result.Should().NotBeNull();
        result!.Name.Should().Be("alpha");
        result.Count.Should().Be(7);
    }

    [Fact]
    public async Task L1_temizlenince_L2_den_backfill_olmali()
    {
        await _cache.SetAsync("k-backfill", new TestDto("beta", 3), new CacheOptions(TimeSpan.FromMinutes(1)));

        // L1'i bypass — direkt yeni MemoryCache + aynı Redis multiplexer ile yeni store
        using var freshL1 = new MemoryCache(new MemoryCacheOptions());
        var freshStore = new HybridCacheStore(_redis.Multiplexer, freshL1, NullLogger<HybridCacheStore>.Instance, "test-instance-fresh");

        var result = await freshStore.GetAsync<TestDto>("k-backfill");

        result.Should().NotBeNull();
        result!.Name.Should().Be("beta");
    }

    [Fact]
    public async Task RemoveAsync_hem_L1_hem_L2_silmeli()
    {
        await _cache.SetAsync("k-rm", new TestDto("gamma", 1), new CacheOptions(TimeSpan.FromMinutes(1)));

        await _cache.RemoveAsync("k-rm");

        // Aynı store: L1 + L2 her ikisi de silindi
        var sameStore = await _cache.GetAsync<TestDto>("k-rm");
        sameStore.Should().BeNull();

        // Bağımsız store: aynı Redis, taze L1 — yine null olmalı (L2 silindi)
        using var freshL1 = new MemoryCache(new MemoryCacheOptions());
        var freshStore = new HybridCacheStore(_redis.Multiplexer, freshL1, NullLogger<HybridCacheStore>.Instance, "test-instance-fresh");
        var freshGet = await freshStore.GetAsync<TestDto>("k-rm");
        freshGet.Should().BeNull();
    }

    [Fact]
    public async Task RemoveByPrefix_alti_tum_keyleri_silmeli()
    {
        await _cache.SetAsync("prefix:a", new TestDto("a", 1), new CacheOptions(TimeSpan.FromMinutes(1)));
        await _cache.SetAsync("prefix:b", new TestDto("b", 2), new CacheOptions(TimeSpan.FromMinutes(1)));
        await _cache.SetAsync("other:x", new TestDto("x", 9), new CacheOptions(TimeSpan.FromMinutes(1)));

        await _cache.RemoveByPrefixAsync("prefix:");

        (await _cache.GetAsync<TestDto>("prefix:a")).Should().BeNull();
        (await _cache.GetAsync<TestDto>("prefix:b")).Should().BeNull();
        (await _cache.GetAsync<TestDto>("other:x")).Should().NotBeNull(); // dokunulmamış olmalı
    }

    [Fact]
    public async Task GetOrCreate_miss_factory_calistirip_set_etmeli()
    {
        var factoryCalls = 0;
        var result = await _cache.GetOrCreateAsync(
            "k-create",
            ct =>
            {
                factoryCalls++;
                return Task.FromResult(new TestDto("delta", 5));
            },
            new CacheOptions(TimeSpan.FromMinutes(1)));

        result.Name.Should().Be("delta");
        factoryCalls.Should().Be(1);

        // Tekrar çağrı → cache hit → factory tetiklenmez
        var second = await _cache.GetOrCreateAsync(
            "k-create",
            ct =>
            {
                factoryCalls++;
                return Task.FromResult(new TestDto("nope", -1));
            },
            new CacheOptions(TimeSpan.FromMinutes(1)));

        second.Name.Should().Be("delta");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreate_paralel_cagri_stampede_korumasi()
    {
        var factoryCalls = 0;
        var lockObj = new Lock();

        var tasks = Enumerable.Range(0, 10).Select(_ => _cache.GetOrCreateAsync(
            "k-stampede",
            async ct =>
            {
                lock (lockObj) factoryCalls++;
                await Task.Delay(50, ct); // simüle slow factory
                return new TestDto("once", 1);
            },
            new CacheOptions(TimeSpan.FromMinutes(1)))).ToArray();

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r.Name.Should().Be("once"));
        factoryCalls.Should().Be(1, "stampede koruması paralel factory çağrısını engellemeli");
    }
}
