namespace CleanTenant.Application.Common.Caching;

/// <summary>
/// <para>
/// MediatR <c>IRequest</c> (özellikle Query) tipi sınıflara konularak
/// <see cref="Pipeline.CachingBehavior{TRequest,TResponse}"/> tarafından okunur.
/// Cache key template'i + TTL preset'i taşır.
/// </para>
/// <para>
/// <b>Key template:</b> <c>{PropertyName}</c> placeholder'ları request'in
/// property'lerinden reflection ile resolve edilir. Örnek:
/// </para>
/// <code>
/// [Cacheable("catalog:tenants:by-id:{Id}", CacheTtlPreset.DetailMediumLived)]
/// public sealed record GetTenantByIdQuery(Guid Id) : IRequest&lt;Result&lt;TenantDto&gt;&gt;;
/// </code>
/// <para>
/// Final cache key: <c>cleantenant:v1:mediatr:catalog:tenants:by-id:{guid}</c>.
/// Behavior key prefix'ini ekler — query'lerin generic cache namespace'inden
/// reader pattern'ininkiyle çakışmasını önler.
/// </para>
/// <para>
/// <b>Cache disiplini:</b> Yalnız Query'lere ekle, Command'lara değil. Failure
/// sonuçları cache'lenmez (Behavior <c>IsSuccess</c> kontrol eder).
/// </para>
/// </summary>
/// <param name="keyTemplate">Cache key şablonu. <c>{PropName}</c> placeholder'ları request'ten doldurulur.</param>
/// <param name="ttl">TTL preset'i.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CacheableAttribute(string keyTemplate, CacheTtlPreset ttl) : Attribute
{
    /// <summary>Cache key şablonu (placeholder'lı).</summary>
    public string KeyTemplate { get; } = keyTemplate;

    /// <summary>TTL preset enum'u.</summary>
    public CacheTtlPreset Ttl { get; } = ttl;
}
