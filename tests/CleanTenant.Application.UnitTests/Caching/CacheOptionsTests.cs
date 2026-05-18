using CleanTenant.Application.Common.Caching;

namespace CleanTenant.Application.UnitTests.Caching;

/// <summary>
/// <see cref="CacheOptions"/> default preset'lerinin sabitliği. Her preset
/// dakika cinsinden net bir TTL taşır; cache invalidation davranışı
/// preset değişirse beklenmedik yönde değişebilir.
/// </summary>
public sealed class CacheOptionsTests
{
    [Fact]
    public void ListShortLived_5_dakika_olmali()
    {
        CacheOptions.ListShortLived.AbsoluteExpiration.Should().Be(TimeSpan.FromMinutes(5));
        CacheOptions.ListShortLived.SlidingExpiration.Should().BeNull();
    }

    [Fact]
    public void DetailMediumLived_10_dakika_olmali()
    {
        CacheOptions.DetailMediumLived.AbsoluteExpiration.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void ReferenceLongLived_30_dakika_olmali()
    {
        CacheOptions.ReferenceLongLived.AbsoluteExpiration.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void Custom_options_olusturulabilmeli()
    {
        var opts = new CacheOptions(TimeSpan.FromHours(1), TimeSpan.FromMinutes(5), ["tenants", "v2"]);
        opts.AbsoluteExpiration.Should().Be(TimeSpan.FromHours(1));
        opts.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(5));
        opts.Tags.Should().BeEquivalentTo(["tenants", "v2"]);
    }
}
