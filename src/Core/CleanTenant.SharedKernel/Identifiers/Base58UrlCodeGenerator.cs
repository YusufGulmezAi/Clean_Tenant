namespace CleanTenant.SharedKernel.Identifiers;

/// <summary>
/// <para>
/// 9 karakterlik Base58 URL kodu üreten <see cref="IUrlCodeGenerator"/>
/// implementasyonu.
/// </para>
/// <para>
/// <b>Alfabe:</b> <c>1-9 A-H J-N P-Z a-k m-z</c> (görsel olarak karışan
/// <c>0</c>, <c>O</c>, <c>I</c>, <c>l</c> karakterleri hariç). 58 karakter ×
/// 9 pozisyon ≈ 1.85 × 10¹⁵ kombinasyon.
/// </para>
/// <para>
/// <b>Algoritma:</b> <see cref="Guid.NewGuid"/>'in ilk 8 byte'ı (64 bit
/// rastgelelik) Base58 tabanında çözülüp ilk 9 karakter alınır. GUID v4
/// distribütif olduğu için çıktı da yaklaşık eşit dağılımdadır.
/// </para>
/// <para>
/// <b>Çakışma:</b> Bu sınıf çakışma kontrolü yapmaz; DB'deki unique
/// constraint son güvence olarak SaveChangesInterceptor retry mantığını
/// (Faz v0.1.7) tetikler. Pratikte 10⁻¹⁵ seviyesinde olduğundan retry
/// neredeyse hiç çalışmaz.
/// </para>
/// </summary>
public sealed class Base58UrlCodeGenerator : IUrlCodeGenerator
{
    /// <summary>Base58 alfabesi (Bitcoin standardı).</summary>
    private const string Alphabet =
        "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    /// <summary>Üretilen kodun karakter uzunluğu.</summary>
    private const int CodeLength = 9;

    /// <inheritdoc />
    public string Generate()
    {
        // GUID'in ilk 8 byte'ından 64-bit unsigned sayı türet — GUID v4'ün
        // rastgele bitlerinin önemli kısmı bu aralıkta.
        Span<byte> bytes = stackalloc byte[16];
        _ = Guid.NewGuid().TryWriteBytes(bytes);
        var number = BitConverter.ToUInt64(bytes);

        // Base58 tabanında modulo ile 9 karakter üret (sağdan sola).
        Span<char> result = stackalloc char[CodeLength];
        for (var i = CodeLength - 1; i >= 0; i--)
        {
            result[i] = Alphabet[(int)(number % 58)];
            number /= 58;
        }

        return new string(result);
    }
}
