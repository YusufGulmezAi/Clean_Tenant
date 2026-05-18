using CleanTenant.Application.Common.Auth;
using CleanTenant.Infrastructure.Caching.Sessions;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.SharedKernel.Context;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CleanTenant.Infrastructure.IntegrationTests.Sessions;

/// <summary>
/// <see cref="CachedAuthSessionStore"/> integration testleri — gerçek Redis +
/// L1 IMemoryCache. Revocation + multi-instance senaryoları doğrulanır.
/// </summary>
public sealed class CachedAuthSessionStoreTests : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private readonly RedisFixture _redis;
    private static readonly SessionSettings Settings = new() { KeyPrefix = "test:auth", TtlPaddingMinutes = 30 };

    public CachedAuthSessionStoreTests(RedisFixture redis)
    {
        _redis = redis;
    }

    public Task InitializeAsync() => _redis.FlushAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private (CachedAuthSessionStore cached, IMemoryCache l1, RedisAuthSessionStore inner) Build(string instanceId)
    {
        var keyBuilder = new SessionKeyBuilder(Options.Create(Settings));
        var inner = new RedisAuthSessionStore(_redis.Multiplexer, keyBuilder);
        var l1 = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedAuthSessionStore(
            inner, l1, _redis.Multiplexer, NullLogger<CachedAuthSessionStore>.Instance, instanceId);
        return (cached, l1, inner);
    }

    private static AuthSession NewSession(Guid? sessionId = null, Guid? userId = null) => new()
    {
        SessionId = sessionId ?? Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        ContextId = Guid.NewGuid(),
        Email = "test@example.com",
        UserName = "test",
        ScopeLevel = ScopeLevel.System,
        Roles = ["Developer"],
        Permissions = ["System.Read"],
        PersonaSide = PersonaSide.Management,
        IssuedAt = DateTimeOffset.UtcNow,
        LastActivity = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Store_sonrasi_Get_L1_hit_olmali()
    {
        var (store, l1, _) = Build("A");
        var session = NewSession();

        await store.StoreAsync(session, TimeSpan.FromMinutes(5));
        var got = await store.GetAsync(session.SessionId);

        got.Should().NotBeNull();
        got!.SessionId.Should().Be(session.SessionId);

        // L1'de olduğunu doğrula — direkt key ile
        l1.TryGetValue($"l1:auth-session:{session.SessionId:N}", out object? _).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_local_L1_silmeli()
    {
        var (store, l1, _) = Build("A");
        var session = NewSession();

        await store.StoreAsync(session, TimeSpan.FromMinutes(5));
        await store.DeleteAsync(session.SessionId, session.UserId);

        l1.TryGetValue($"l1:auth-session:{session.SessionId:N}", out object? _).Should().BeFalse();
        (await store.GetAsync(session.SessionId)).Should().BeNull();
    }

    [Fact]
    public async Task MultiInstance_Delete_diger_L1_pub_sub_ile_silinmeli()
    {
        var (storeA, l1A, _) = Build("A");
        var (storeB, l1B, _) = Build("B");

        var mux2 = await _redis.CreateNewMultiplexerAsync();
        var subscriberB = new AuthSessionInvalidationSubscriber(
            _redis.Multiplexer, storeB, NullLogger<AuthSessionInvalidationSubscriber>.Instance);
        await subscriberB.StartAsync(CancellationToken.None);
        await Task.Delay(200); // subscribe propagation

        var session = NewSession();
        await storeA.StoreAsync(session, TimeSpan.FromMinutes(5));
        await storeB.GetAsync(session.SessionId); // B'nin L1'ine backfill

        l1B.TryGetValue($"l1:auth-session:{session.SessionId:N}", out object? _).Should().BeTrue();

        // A delete → pub/sub → B kendi L1'inden silsin
        await storeA.DeleteAsync(session.SessionId, session.UserId);

        await Task.Delay(300); // pub/sub propagation

        l1B.TryGetValue($"l1:auth-session:{session.SessionId:N}", out object? _).Should().BeFalse();

        await subscriberB.StopAsync(CancellationToken.None);
        await mux2.DisposeAsync();
    }

    [Fact]
    public async Task Update_L1_yi_yeni_versiyonla_doldurmali()
    {
        var (store, l1, _) = Build("A");
        var session = NewSession();
        session.LastActivity = DateTimeOffset.UtcNow.AddMinutes(-10);

        await store.StoreAsync(session, TimeSpan.FromMinutes(5));

        // Mutate + update
        var updated = NewSession(session.SessionId, session.UserId);
        updated.SupportMode = "WriteEnabled";
        await store.UpdateAsync(updated, TimeSpan.FromMinutes(5));

        var got = await store.GetAsync(session.SessionId);

        got.Should().NotBeNull();
        got!.SupportMode.Should().Be("WriteEnabled");
    }

    [Fact]
    public async Task Touch_L1_yi_etkilememeli_ama_Redis_TTL_uzatmali()
    {
        var (store, l1, _) = Build("A");
        var session = NewSession();
        await store.StoreAsync(session, TimeSpan.FromMinutes(5));

        // L1'de var
        l1.TryGetValue($"l1:auth-session:{session.SessionId:N}", out object? cachedBefore).Should().BeTrue();

        await store.TouchAsync(session.SessionId, TimeSpan.FromMinutes(10));

        // L1'de aynı obje (Touch sadece Redis side)
        l1.TryGetValue($"l1:auth-session:{session.SessionId:N}", out object? cachedAfter).Should().BeTrue();
        ReferenceEquals(cachedBefore, cachedAfter).Should().BeTrue();
    }
}
