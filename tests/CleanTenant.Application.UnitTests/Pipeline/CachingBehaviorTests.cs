using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Pipeline;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Application.UnitTests.Pipeline;

/// <summary>
/// <see cref="CachingBehavior{TRequest,TResponse}"/> davranış testleri.
/// </summary>
public sealed class CachingBehaviorTests
{
    public sealed record UncachedQuery : IRequest<Result<string>>;

    [Cacheable("test:cached:{Id}", CacheTtlPreset.ListShortLived)]
    public sealed record CachedQuery(Guid Id) : IRequest<Result<string>>;

    [Cacheable("test:bad:{Missing}", CacheTtlPreset.ListShortLived)]
    public sealed record BrokenTemplateQuery : IRequest<Result<string>>;

    [Fact]
    public async Task Cacheable_yoksa_pass_through()
    {
        var cache = Substitute.For<ICacheStore>();
        var logger = Substitute.For<ILogger<CachingBehavior<UncachedQuery, Result<string>>>>();
        var behavior = new CachingBehavior<UncachedQuery, Result<string>>(cache, logger);

        var result = await behavior.Handle(
            new UncachedQuery(),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await cache.DidNotReceive().GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<Result<string>>(), Arg.Any<CacheOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cache_hit_factory_calistirilmamali()
    {
        var cache = Substitute.For<ICacheStore>();
        var logger = Substitute.For<ILogger<CachingBehavior<CachedQuery, Result<string>>>>();
        var behavior = new CachingBehavior<CachedQuery, Result<string>>(cache, logger);
        var query = new CachedQuery(Guid.NewGuid());

        cache.GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("cached-value"));

        var factoryCalled = false;
        var result = await behavior.Handle(
            query,
            () =>
            {
                factoryCalled = true;
                return Task.FromResult(Result<string>.Success("fresh"));
            },
            CancellationToken.None);

        factoryCalled.Should().BeFalse();
        result.Value.Should().Be("cached-value");
        await cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<Result<string>>(), Arg.Any<CacheOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cache_miss_factory_calismali_ve_basari_cache_lenmeli()
    {
        var cache = Substitute.For<ICacheStore>();
        var logger = Substitute.For<ILogger<CachingBehavior<CachedQuery, Result<string>>>>();
        var behavior = new CachingBehavior<CachedQuery, Result<string>>(cache, logger);
        var query = new CachedQuery(Guid.NewGuid());

        cache.GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Result<string>?)null);

        var result = await behavior.Handle(
            query,
            () => Task.FromResult(Result<string>.Success("fresh")),
            CancellationToken.None);

        result.Value.Should().Be("fresh");
        await cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Is<Result<string>>(r => r.IsSuccess && r.Value == "fresh"),
            Arg.Any<CacheOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failure_sonucu_cache_lenmemeli()
    {
        var cache = Substitute.For<ICacheStore>();
        var logger = Substitute.For<ILogger<CachingBehavior<CachedQuery, Result<string>>>>();
        var behavior = new CachingBehavior<CachedQuery, Result<string>>(cache, logger);
        var query = new CachedQuery(Guid.NewGuid());

        cache.GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Result<string>?)null);

        var failure = Result<string>.Failure(Error.NotFound("NOT-FOUND", "yok"));
        var result = await behavior.Handle(
            query,
            () => Task.FromResult(failure),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<Result<string>>(), Arg.Any<CacheOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Bozuk_template_fail_open_handler_calismali()
    {
        var cache = Substitute.For<ICacheStore>();
        var logger = Substitute.For<ILogger<CachingBehavior<BrokenTemplateQuery, Result<string>>>>();
        var behavior = new CachingBehavior<BrokenTemplateQuery, Result<string>>(cache, logger);

        var result = await behavior.Handle(
            new BrokenTemplateQuery(),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Cache erişimi yapılmamış (resolve fail → erkenden next'e gidiyor)
        await cache.DidNotReceive().GetAsync<Result<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
