namespace CleanTenant.Application.Common.Security;

/// <summary>
/// PII (TCKN/VKN/telefon) alanlarını UI'ye dönerken maskeler. <c>[Sensitive]</c>
/// attribute yalnız audit delta'sını maskeler; bu helper, <c>tenant.party.pii.view</c>
/// izni olmayan kullanıcıya gösterim için kullanılır. Maskeleme sorgu/handler
/// seviyesinde uygulanır (ham PII DTO'ya hiç çıkmaz).
/// </summary>
public static class PiiMasker
{
    /// <summary>TCKN maskesi — ilk 3 + son 2 görünür (örn. <c>123••••••90</c>).</summary>
    public static string? MaskTckn(string? value) => MaskMiddle(value, 3, 2);

    /// <summary>VKN maskesi — ilk 3 + son 1 görünür.</summary>
    public static string? MaskVkn(string? value) => MaskMiddle(value, 3, 1);

    /// <summary>Telefon maskesi — ilk 5 + son 2 görünür (örn. <c>+90 5••••••47</c>).</summary>
    public static string? MaskPhone(string? value) => MaskMiddle(value, 5, 2);

    /// <summary>
    /// Ortayı maskeler: baştan <paramref name="keepStart"/>, sondan
    /// <paramref name="keepEnd"/> karakter korunur, arası '•' ile doldurulur.
    /// Kısa değerlerde tamamı maskelenir.
    /// </summary>
    public static string? MaskMiddle(string? value, int keepStart, int keepEnd)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var v = value.Trim();
        if (v.Length <= keepStart + keepEnd)
            return new string('•', v.Length);
        var middle = new string('•', v.Length - keepStart - keepEnd);
        return string.Concat(v.AsSpan(0, keepStart), middle, v.AsSpan(v.Length - keepEnd));
    }
}
