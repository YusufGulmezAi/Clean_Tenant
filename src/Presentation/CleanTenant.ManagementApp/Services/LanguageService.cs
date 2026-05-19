using System.Globalization;

namespace CleanTenant.ManagementApp.Services;

/// <summary>Uygulama tarafından desteklenen dillerin listesi ve yardımcı metotlar.</summary>
public static class LanguageService
{
    /// <summary>Desteklenen dil seçenekleri (TR varsayılan; AR sağdan-sola).</summary>
    public static readonly IReadOnlyList<LanguageOption> Supported =
    [
        new("tr-TR", "🇹🇷", "Türkçe"),
        new("en-US", "🇺🇸", "English"),
        new("de-DE", "🇩🇪", "Deutsch"),
        new("ru-RU", "🇷🇺", "Русский"),
        new("ar-SA", "🇸🇦", "العربية", IsRtl: true),
    ];

    /// <summary>Verilen koda karşılık gelen seçeneği döner; bulamazsa ilk (TR) döner.</summary>
    public static LanguageOption Resolve(string? code)
        => Supported.FirstOrDefault(l =>
               string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase))
           ?? Supported[0];

    /// <summary>
    /// v0.2.10.f — Aktif UI kültürü RTL mi? <see cref="CultureInfo.CurrentUICulture"/>
    /// üzerinden çözer. MainLayout <c>MudRtlProvider.RightToLeft</c> bağlamasında ve
    /// <c>&lt;body dir="..."&gt;</c> JS interop'unda kullanılır.
    /// </summary>
    public static bool IsCurrentCultureRtl()
        => Resolve(CultureInfo.CurrentUICulture.Name).IsRtl;
}

/// <summary>Dil seçeneği — BCP-47 kültür kodu, bayrak emoji ve görünen ad.</summary>
/// <param name="Code">Örn. "tr-TR", "en-US".</param>
/// <param name="Flag">Ülke bayrağı emoji.</param>
/// <param name="Name">Kullanıcıya gösterilen dil adı.</param>
/// <param name="IsRtl">Sağdan-sola (Arapça gibi) ise true.</param>
public record LanguageOption(string Code, string Flag, string Name, bool IsRtl = false);
