using System.Text.RegularExpressions;

namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Login isteğindeki <c>Identifier</c> alanının tipini ve normalize formunu
/// tespit eder. Dört tip desteklenir:
/// </para>
/// <list type="bullet">
///   <item><see cref="LoginIdentifierType.Email"/> — <c>@</c> içerir.</item>
///   <item><see cref="LoginIdentifierType.Tckn"/> — 11 haneli sadece rakam + Mernis checksum.
///   <b>YKN</b> (Yabancı Kimlik Numarası) da bu tipte kabul edilir; ilk hanesi 9'la başlar
///   ama TCKN ile aynı checksum'a uyar.</item>
///   <item><see cref="LoginIdentifierType.Vkn"/> — 10 haneli sadece rakam, ilk hane 1-9.</item>
///   <item><see cref="LoginIdentifierType.PhoneNumber"/> — uluslararası format normalize
///   edilir (<c>+90...</c>).</item>
/// </list>
/// </summary>
public static partial class LoginIdentifier
{
    [GeneratedRegex(@"^[\d\s\-\+\(\)]+$", RegexOptions.Compiled)]
    private static partial Regex PhoneCharsRegex();

    [GeneratedRegex(@"\D", RegexOptions.Compiled)]
    private static partial Regex NonDigitRegex();

    /// <summary>
    /// Verilen identifier'ı tip ve normalize formuyla çözer.
    /// </summary>
    /// <param name="raw">Kullanıcının girdiği identifier (email / TCKN / telefon).</param>
    /// <returns>(Tip, Normalize edilmiş değer); hiçbiri uymazsa Unknown.</returns>
    public static (LoginIdentifierType Type, string Normalized) Resolve(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (LoginIdentifierType.Unknown, string.Empty);
        }

        var trimmed = raw.Trim();

        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            return (LoginIdentifierType.Email, trimmed.ToLowerInvariant());
        }

        // Önce TCKN/YKN denemesi (11 hane + checksum)
        if (TryParseTckn(trimmed, out var tckn))
        {
            return (LoginIdentifierType.Tckn, tckn);
        }

        // Sonra telefon (5xx başlayan 10 hane veya alternatif formatlar).
        // VKN'den ÖNCE çünkü "5551234567" hem 10-hane sayı (VKN gibi görünür)
        // hem 10-hane mobil numara. Telefon olarak öncelikli kabul edilir.
        if (TryNormalizePhone(trimmed, out var phone))
        {
            return (LoginIdentifierType.PhoneNumber, phone);
        }

        // VKN denemesi (10 hane, ilk hane 1-9, mobil olmayan)
        if (TryParseVkn(trimmed, out var vkn))
        {
            return (LoginIdentifierType.Vkn, vkn);
        }

        return (LoginIdentifierType.Unknown, trimmed);
    }

    /// <summary>
    /// 10 haneli rakam, ilk hane 0 değilse VKN olarak kabul edilir.
    /// Checksum algoritması tartışmalı (Gelir İdaresi standardı tek değil);
    /// şimdilik sadece format kontrolü yapılır. İleride algoritma eklenebilir.
    /// </summary>
    public static bool TryParseVkn(string input, out string normalized)
    {
        normalized = string.Empty;
        if (input.Length != 10)
        {
            return false;
        }
        if (!input.All(char.IsDigit))
        {
            return false;
        }
        if (input[0] == '0')
        {
            return false;
        }
        normalized = input;
        return true;
    }

    /// <summary>
    /// 11 haneli rakam ise ve TCKN checksum algoritmasını geçiyorsa true.
    /// </summary>
    public static bool TryParseTckn(string input, out string normalized)
    {
        normalized = string.Empty;
        if (input.Length != 11)
        {
            return false;
        }
        if (!input.All(char.IsDigit))
        {
            return false;
        }
        if (input[0] == '0')
        {
            return false;
        }

        var digits = input.Select(c => c - '0').ToArray();

        // Hane 10: (1+3+5+7+9) * 7 - (2+4+6+8) → mod 10 = digits[9]
        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        var d10 = ((oddSum * 7) - evenSum) % 10;
        if (d10 < 0) d10 += 10;
        if (d10 != digits[9])
        {
            return false;
        }

        // Hane 11: digits 1-10 toplamı mod 10
        var totalSum = digits.Take(10).Sum();
        var d11 = totalSum % 10;
        if (d11 != digits[10])
        {
            return false;
        }

        normalized = input;
        return true;
    }

    /// <summary>
    /// Telefon numarasını <c>+90XXXXXXXXXX</c> formatına normalize eder.
    /// Kabul edilen girdiler: <c>5xx...</c>, <c>05xx...</c>, <c>+905xx...</c>,
    /// <c>905xx...</c>, içlerinde boşluk / tire / parantez olabilir.
    /// </summary>
    public static bool TryNormalizePhone(string input, out string normalized)
    {
        normalized = string.Empty;

        if (!PhoneCharsRegex().IsMatch(input))
        {
            return false;
        }

        // Tüm rakam dışı karakterleri kaldır
        var digitsOnly = NonDigitRegex().Replace(input, string.Empty);

        // Olası baş: "90...", "0...", "5..." 10 hane
        if (digitsOnly.StartsWith("90", StringComparison.Ordinal) && digitsOnly.Length == 12)
        {
            digitsOnly = digitsOnly[2..];
        }
        else if (digitsOnly.StartsWith('0') && digitsOnly.Length == 11)
        {
            digitsOnly = digitsOnly[1..];
        }

        if (digitsOnly.Length != 10 || !digitsOnly.StartsWith('5'))
        {
            return false;
        }

        normalized = "+90" + digitsOnly;
        return true;
    }
}

/// <summary>Login identifier'ı tipi.</summary>
public enum LoginIdentifierType
{
    /// <summary>Belirsiz / tanımlanamayan format.</summary>
    Unknown = 0,
    /// <summary>E-posta (<c>@</c> içerir).</summary>
    Email = 1,
    /// <summary>
    /// 11 haneli T.C. Kimlik Numarası veya Yabancı Kimlik Numarası
    /// (Mernis checksum doğrulanmış).
    /// </summary>
    Tckn = 2,
    /// <summary>10 haneli Vergi Kimlik Numarası (format kontrolü).</summary>
    Vkn = 4,
    /// <summary>Türkiye cep telefonu numarası, +90 5xx... formatında normalize.</summary>
    PhoneNumber = 3,
}
