using CleanTenant.Application.Common.Caching;

namespace CleanTenant.Application.UnitTests.Caching;

/// <summary>
/// <see cref="CacheKeys"/> convention'ının deterministik + prefix tutarlılığını
/// doğrular. Yanlış key üretimi cache hit/miss sorunlarına ve invalidation
/// kaymalarına yol açar; bu nedenle key'ler statik test'lerle kilitlenir.
/// </summary>
public sealed class CacheKeysTests
{
    /// <summary>Versioned root prefix sabitliği.</summary>
    [Fact]
    public void KeyPrefix_versiyon_sabiti_korunmali()
    {
        CacheKeys.KeyPrefix.Should().Be("cleantenant:v1");
    }

    /// <summary>Tenant prefix root altında.</summary>
    [Fact]
    public void Tenant_prefix_root_altinda_olmali()
    {
        CacheKeys.Tenant.Prefix.Should().StartWith(CacheKeys.KeyPrefix + ":");
        CacheKeys.Tenant.Prefix.Should().Be("cleantenant:v1:catalog:tenants");
    }

    /// <summary>Tenant.AllActive deterministik string.</summary>
    [Fact]
    public void Tenant_AllActive_deterministik_olmali()
    {
        CacheKeys.Tenant.AllActive.Should().Be("cleantenant:v1:catalog:tenants:all-active");
    }

    /// <summary>Tenant.ById GUID formatı "N" (32 hex, separator yok).</summary>
    [Fact]
    public void Tenant_ById_N_format_kullanmali()
    {
        var id = new Guid("12345678-1234-1234-1234-123456789012");
        CacheKeys.Tenant.ById(id).Should().Be("cleantenant:v1:catalog:tenants:by-id:12345678123412341234123456789012");
    }

    /// <summary>Company prefix root altında.</summary>
    [Fact]
    public void Company_prefix_root_altinda_olmali()
    {
        CacheKeys.Company.Prefix.Should().StartWith(CacheKeys.KeyPrefix + ":");
        CacheKeys.Company.Prefix.Should().Be("cleantenant:v1:main:companies");
    }

    /// <summary>Company.ByTenant deterministik.</summary>
    [Fact]
    public void Company_ByTenant_N_format_kullanmali()
    {
        var tid = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CacheKeys.Company.ByTenant(tid).Should().Be("cleantenant:v1:main:companies:by-tenant:aaaaaaaabbbbccccddddeeeeeeeeeeee");
    }

    /// <summary>User.Contexts deterministik.</summary>
    [Fact]
    public void User_Contexts_N_format_kullanmali()
    {
        var uid = new Guid("11111111-2222-3333-4444-555555555555");
        CacheKeys.User.Contexts(uid).Should().Be("cleantenant:v1:catalog:user:contexts:11111111222233334444555555555555");
    }

    /// <summary>Invalidation channel sabit.</summary>
    [Fact]
    public void InvalidationChannel_root_altinda_olmali()
    {
        CacheKeys.InvalidationChannel.Should().Be("cleantenant:v1:cache-invalidate");
    }
}
