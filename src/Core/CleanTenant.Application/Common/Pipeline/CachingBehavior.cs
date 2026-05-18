using System.Reflection;
using CleanTenant.Application.Common.Caching;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Application.Common.Pipeline;

/// <summary>
/// <para>
/// MediatR pipeline behavior'ı — <see cref="CacheableAttribute"/> taşıyan Query
/// tipindeki request'leri <see cref="ICacheStore"/> üzerinden cache'ler.
/// Attribute yoksa pass-through.
/// </para>
/// <para>
/// <b>Cache key</b> attribute'ın <c>KeyTemplate</c>'inden <c>{Property}</c>
/// placeholder'ları resolve edilerek üretilir
/// (<see cref="CacheKeyTemplateResolver"/>). <b>TTL</b>
/// <see cref="CacheableAttribute.Ttl"/> preset'inden gelir.
/// </para>
/// <para>
/// <b>Failure cache koruması:</b> Handler <c>Result</c> / <c>Result&lt;T&gt;</c>
/// dönerse ve <c>IsSuccess == false</c> ise sonuç cache'lenmez — cache miss
/// path'inden direkt döner. Diğer response tiplerinde her sonuç cache'lenir
/// (caller sorumluluğu — Cacheable yalnız Query'lere eklenmeli).
/// </para>
/// <para>
/// Pipeline sırası: <c>AuthorizationBehavior</c> → <b><c>CachingBehavior</c></b>
/// → <c>ValidationBehavior</c> → <c>LoggingBehavior</c>. Yetkisizlik cache
/// öncesi reddedilir; validation cache miss path'inde tetiklenir.
/// </para>
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheStore _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    private static readonly PropertyInfo? IsSuccessProperty = typeof(TResponse).GetProperty(
        "IsSuccess",
        BindingFlags.Public | BindingFlags.Instance);

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CachingBehavior(ICacheStore cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheable = typeof(TRequest).GetCustomAttribute<CacheableAttribute>(inherit: false);
        if (cacheable is null)
        {
            return await next();
        }

        string key;
        try
        {
            key = CacheKeyTemplateResolver.Resolve(cacheable.KeyTemplate, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cacheable key template resolve edilemedi: {Type}", typeof(TRequest).Name);
            // Cache başarısızlığında handler'ı kıralım — fail-open.
            return await next();
        }

        var options = cacheable.Ttl.ToOptions();

        // Manuel cache-aside: GetOrCreate failure'u da cache'liyor; biz Result.IsSuccess
        // kontrolüyle yalnız success cache'lemek istiyoruz.
        var cached = await _cache.GetAsync<TResponse>(key, cancellationToken);
        if (cached is not null && IsSuccessOrUnknown(cached))
        {
            return cached;
        }

        var result = await next();

        if (result is not null && IsSuccessOrUnknown(result))
        {
            await _cache.SetAsync(key, result, options, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Result tipinde <c>IsSuccess == false</c> ise cache'leme. Result değilse
    /// (örn. raw DTO döndüren handler) her sonuç cache'lenir.
    /// </summary>
    private static bool IsSuccessOrUnknown(TResponse response)
    {
        if (IsSuccessProperty is null) return true;
        var value = IsSuccessProperty.GetValue(response);
        if (value is bool b) return b;
        return true;
    }
}
