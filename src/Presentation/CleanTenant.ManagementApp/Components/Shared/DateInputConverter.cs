using System.Globalization;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// MudDatePicker için ortak tarih giriş converter'ı fabrikası.
/// <para>
/// <c>Editable="true"</c> ile birlikte kullanılır: kullanıcı tarihi elle yazabilir
/// ve <c>"."</c>, <c>"/"</c>, <c>"-"</c> ayraçlarını serbestçe kullanabilir; ekranda
/// daima <c>gg.aa.yyyy</c> (tr-TR) gösterilir. Geçersiz girişte Türkçe hata döner.
/// </para>
/// <para>
/// Her MudDatePicker kendi örneğini kullanmalı (GetError durumu örnek-bazlıdır);
/// bu yüzden <see cref="Create"/> her çağrıda yeni bir converter döndürür.
/// </para>
/// </summary>
public static class DateInputConverter
{
    /// <summary>Kabul edilen parse formatları — tek/çift haneli gün-ay desteklenir.</summary>
    private static readonly string[] InputFormats = ["dd.MM.yyyy", "d.M.yyyy"];

    /// <summary>Yeni bir esnek tarih converter'ı üretir.</summary>
    /// <param name="invalidMessage">Geçersiz tarih hatası metni (varsayılan Türkçe).</param>
    public static MudBlazor.Converter<DateTime?, string> Create(
        string invalidMessage = "Geçersiz tarih. Örnek: 01.01.2026")
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var converter = new MudBlazor.Converter<DateTime?, string> { Culture = culture };

        converter.SetFunc = date => date?.ToString("dd.MM.yyyy", culture);
        converter.GetFunc = text =>
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                converter.GetError = false;
                return null;
            }

            // "." / "/" / "-" → "." normalize et, sonra parse et.
            var normalized = text.Trim().Replace('/', '.').Replace('-', '.');
            if (DateTime.TryParseExact(normalized, InputFormats, culture,
                    DateTimeStyles.None, out var parsed))
            {
                converter.GetError = false;
                return parsed;
            }

            converter.GetError = true;
            converter.GetErrorMessage = (invalidMessage, Array.Empty<object>());
            return null;
        };

        return converter;
    }
}
