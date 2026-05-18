using System.Reflection;
using System.Text.RegularExpressions;

namespace CleanTenant.Application.Common.Caching;

/// <summary>
/// <para>
/// <see cref="CacheableAttribute.KeyTemplate"/>'deki <c>{PropertyName}</c>
/// placeholder'larını request objesi'nin property değerleriyle değiştirir.
/// Sonuca <see cref="CacheKeys.KeyPrefix"/> + <c>:mediatr:</c> prefix'i eklenir.
/// </para>
/// <para>
/// <b>Format</b>: <c>{KeyPrefix}:mediatr:{resolved}</c>. Örn:
/// <c>cleantenant:v1:mediatr:catalog:tenants:by-id:11112222...</c>.
/// </para>
/// <para>
/// Guid değerler "N" formatında (32 hex, separator yok) serialize edilir;
/// diğer scalar tipler <see cref="object.ToString"/> ile (culture-invariant).
/// Null değerler "null" literal'i olur. Tutarsızlık olmasın diye reflection
/// per-request tipi cache'lenir (PropertyInfo cache).
/// </para>
/// </summary>
public static partial class CacheKeyTemplateResolver
{
    private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = new();
    private static readonly Lock CacheLock = new();

    [GeneratedRegex(@"\{([A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    /// <summary>Template'i request property'leriyle resolve eder ve tam cache key'i döner.</summary>
    public static string Resolve<TRequest>(string keyTemplate, TRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var properties = GetPropertyMap(typeof(TRequest));

        var resolved = PlaceholderRegex().Replace(keyTemplate, match =>
        {
            var propName = match.Groups[1].Value;
            if (!properties.TryGetValue(propName, out var prop))
            {
                throw new InvalidOperationException(
                    $"CacheableAttribute key template '{keyTemplate}' içindeki '{{{propName}}}' " +
                    $"property'si {typeof(TRequest).Name} tipinde bulunamadı.");
            }
            var value = prop.GetValue(request);
            return Format(value);
        });

        return $"{CacheKeys.KeyPrefix}:mediatr:{resolved}";
    }

    private static Dictionary<string, PropertyInfo> GetPropertyMap(Type type)
    {
        lock (CacheLock)
        {
            if (PropertyCache.TryGetValue(type, out var cached))
            {
                return cached;
            }
            var map = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);
            PropertyCache[type] = map;
            return map;
        }
    }

    private static string Format(object? value) => value switch
    {
        null => "null",
        Guid g => g.ToString("N"),
        DateTime dt => dt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null",
    };
}
