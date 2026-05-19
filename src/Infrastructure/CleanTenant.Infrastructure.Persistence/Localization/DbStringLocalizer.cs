using System.Globalization;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Infrastructure.Persistence.Localization;

/// <summary>
/// <para>
/// <see cref="IStringLocalizer"/>'ın DB-backed implementasyonu. Aktif kültür
/// <see cref="CultureInfo.CurrentUICulture"/>'dan alınır; ASP.NET Core'un
/// <c>UseRequestLocalization</c> middleware'i bunu request cookie/header'ından
/// set eder.
/// </para>
/// <para>
/// <b>Fallback zinciri (v0.2.10):</b>
/// </para>
/// <list type="number">
///   <item>Aktif kültürün değeri</item>
///   <item><c>en-US</c> (varsayılan ortak çevirilebilir baz)</item>
///   <item><c>tr-TR</c> (geliştirme dili, daima dolu)</item>
///   <item><c>[Key]</c> raw — dev için uyarı (eksik çeviri belirtisi)</item>
/// </list>
/// </summary>
public sealed class DbStringLocalizer : IStringLocalizer
{
    private const string DefaultCulture = "tr-TR";
    private const string EnglishCulture = "en-US";

    private readonly LocalizationStore _store;

    /// <summary>DI bağımlılığını alır.</summary>
    public DbStringLocalizer(LocalizationStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            var (value, notFound) = Resolve(name);
            return new LocalizedString(name, value, notFound);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var (value, notFound) = Resolve(name);
            var formatted = arguments.Length > 0
                ? string.Format(CultureInfo.CurrentCulture, value, arguments)
                : value;
            return new LocalizedString(name, formatted, notFound);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];

    private (string Value, bool NotFound) Resolve(string key)
    {
        var current = CultureInfo.CurrentUICulture.Name;

        // 1) Aktif kültür
        var v = _store.Get(current, key);
        if (v is not null) return (v, false);

        // 2) en-US fallback
        if (!string.Equals(current, EnglishCulture, StringComparison.OrdinalIgnoreCase))
        {
            v = _store.Get(EnglishCulture, key);
            if (v is not null) return (v, false);
        }

        // 3) tr-TR fallback
        if (!string.Equals(current, DefaultCulture, StringComparison.OrdinalIgnoreCase))
        {
            v = _store.Get(DefaultCulture, key);
            if (v is not null) return (v, false);
        }

        // 4) Raw key — dev için uyarı (eksik çeviri)
        return ($"[{key}]", true);
    }
}
