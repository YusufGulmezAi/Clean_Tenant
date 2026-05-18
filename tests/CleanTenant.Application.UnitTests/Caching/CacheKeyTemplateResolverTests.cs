using CleanTenant.Application.Common.Caching;

namespace CleanTenant.Application.UnitTests.Caching;

/// <summary>
/// <see cref="CacheKeyTemplateResolver"/> placeholder resolution davranışı.
/// </summary>
public sealed class CacheKeyTemplateResolverTests
{
    public sealed record SimpleQuery(Guid Id);
    public sealed record MultiQuery(Guid TenantId, int Page, string? Filter);

    [Fact]
    public void Tek_Guid_placeholder_N_formatinda_resolve_olmali()
    {
        var query = new SimpleQuery(new Guid("12345678-1234-1234-1234-123456789012"));

        var key = CacheKeyTemplateResolver.Resolve("catalog:by-id:{Id}", query);

        key.Should().Be("cleantenant:v1:mediatr:catalog:by-id:12345678123412341234123456789012");
    }

    [Fact]
    public void Coklu_placeholder_resolve_olmali()
    {
        var tid = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var query = new MultiQuery(tid, 3, "ali");

        var key = CacheKeyTemplateResolver.Resolve("companies:{TenantId}:p{Page}:f{Filter}", query);

        key.Should().Be("cleantenant:v1:mediatr:companies:aaaaaaaabbbbccccddddeeeeeeeeeeee:p3:fali");
    }

    [Fact]
    public void Null_property_null_literal_olmali()
    {
        var query = new MultiQuery(Guid.Empty, 1, null);

        var key = CacheKeyTemplateResolver.Resolve("f{Filter}", query);

        key.Should().Be("cleantenant:v1:mediatr:fnull");
    }

    [Fact]
    public void Bilinmeyen_property_InvalidOperationException_firlatmali()
    {
        var query = new SimpleQuery(Guid.Empty);

        var act = () => CacheKeyTemplateResolver.Resolve("by:{DoesNotExist}", query);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DoesNotExist*");
    }

    [Fact]
    public void Placeholder_yoksa_template_aynen_donmeli()
    {
        var query = new SimpleQuery(Guid.Empty);

        var key = CacheKeyTemplateResolver.Resolve("static:string", query);

        key.Should().Be("cleantenant:v1:mediatr:static:string");
    }
}
