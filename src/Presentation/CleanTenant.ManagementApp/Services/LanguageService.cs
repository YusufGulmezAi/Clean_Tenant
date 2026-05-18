namespace CleanTenant.ManagementApp.Services;

/// <summary>Uygulama tarafından desteklenen dillerin listesi ve yardımcı metotlar.</summary>
public static class LanguageService
{
    /// <summary>Desteklenen dil seçenekleri (TR varsayılan).</summary>
    public static readonly IReadOnlyList<LanguageOption> Supported =
    [
        new("tr-TR", "🇹🇷", "Türkçe"),
        new("en-US", "🇺🇸", "English"),
        new("de-DE", "🇩🇪", "Deutsch"),
        new("ru-RU", "🇷🇺", "Русский"),
        new("ar-SA", "🇸🇦", "العربية"),
    ];

    /// <summary>Verilen koda karşılık gelen seçeneği döner; bulamazsa ilk (TR) döner.</summary>
    public static LanguageOption Resolve(string? code)
        => Supported.FirstOrDefault(l =>
               string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase))
           ?? Supported[0];
}

/// <summary>Dil seçeneği — BCP-47 kültür kodu, bayrak emoji ve görünen ad.</summary>
/// <param name="Code">Örn. "tr-TR", "en-US".</param>
/// <param name="Flag">Ülke bayrağı emoji.</param>
/// <param name="Name">Kullanıcıya gösterilen dil adı.</param>
public record LanguageOption(string Code, string Flag, string Name);
