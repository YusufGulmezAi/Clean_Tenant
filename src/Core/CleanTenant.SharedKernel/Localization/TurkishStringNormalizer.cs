using System.Globalization;
using System.Text;

namespace CleanTenant.SharedKernel.Localization;

/// <summary>
/// <para>
/// Türkçe karakter farkındalıklı string normalizasyon yardımcısıdır.
/// Arama, eşleşme ve sıralama gibi case-insensitive ve aksan-bağımsız işlemler
/// için kullanılır.
/// </para>
/// <para>
/// <b>Hizalama:</b> Bu sınıfın <see cref="Normalize"/> çıktısı, PostgreSQL'deki
/// <c>unaccent(lower(...))</c> SQL ifadesinin çıktısıyla aynı sonucu vermek
/// üzere tasarlanmıştır. .NET tarafında in-memory bir filtre uygulandığında
/// DB tarafıyla aynı eşleşme davranışı garanti altındadır. Hizalamanın PG
/// kurulumuyla birebir kalması için unit testler her iki çıktıyı kıyaslar
/// (Faz v0.1.4 ile tam doğrulanır).
/// </para>
/// </summary>
public static class TurkishStringNormalizer
{
    /// <summary>Türkçe kültür bilgisi; <c>ToLower</c> davranışını dikkate alır.</summary>
    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Türkçe ve Almanca aksanlı karakterlerin temel Latin harf karşılıkları.
    /// PG <c>unaccent</c>'in ürettiği eşlemeyle birebir.
    /// </summary>
    private static readonly Dictionary<char, char> AccentMap = new()
    {
        // Türkçe
        ['ı'] = 'i',
        ['ş'] = 's',
        ['ğ'] = 'g',
        ['ü'] = 'u',
        ['ö'] = 'o',
        ['ç'] = 'c',
        // Almanca yaygınlar (Almanca da dil setinde)
        ['ä'] = 'a',
        ['ë'] = 'e',
        // Diğer Latin aksan kalıntıları
        ['â'] = 'a',
        ['î'] = 'i',
        ['û'] = 'u',
    };

    /// <summary>
    /// <para>
    /// Türkçe kültüre göre küçük harfe çevirir ve aksanları kaldırır. Sonuç,
    /// case-insensitive ve aksan-bağımsız arama eşleşmesinde kullanılır.
    /// </para>
    /// <para>
    /// Örnekler: <c>"İSTANBUL"</c> → <c>"istanbul"</c>,
    /// <c>"Şişli"</c> → <c>"sisli"</c>, <c>"Çankaya"</c> → <c>"cankaya"</c>,
    /// <c>"Ğümüşhane"</c> → <c>"gumushane"</c>, <c>"Öztürk"</c> → <c>"ozturk"</c>.
    /// </para>
    /// </summary>
    /// <param name="input">Normalize edilecek metin; null veya boş ise boş string döner.</param>
    /// <returns>Lowercase + aksansız metin.</returns>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var lower = input.ToLower(TurkishCulture);
        var builder = new StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            builder.Append(AccentMap.TryGetValue(ch, out var replacement) ? replacement : ch);
        }
        return builder.ToString();
    }

    /// <summary>
    /// Aksanları KORUYARAK Türkçe-uyumlu küçük harfe çevirir. Görüntü amaçlı
    /// (UI'da kullanıcıya gösterirken) kullanılır; arama için
    /// <see cref="Normalize"/> tercih edilir.
    /// </summary>
    /// <param name="input">Küçük harfe çevrilecek metin.</param>
    public static string TurkishLower(string? input)
        => string.IsNullOrEmpty(input) ? string.Empty : input.ToLower(TurkishCulture);
}
