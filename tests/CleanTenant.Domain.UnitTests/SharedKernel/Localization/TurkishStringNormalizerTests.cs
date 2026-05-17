using CleanTenant.SharedKernel.Localization;

namespace CleanTenant.Domain.UnitTests.SharedKernel.Localization;

public sealed class TurkishStringNormalizerTests
{
    [Theory]
    [InlineData("İSTANBUL", "istanbul")]
    [InlineData("Şişli", "sisli")]
    [InlineData("Çankaya", "cankaya")]
    [InlineData("Ğümüşhane", "gumushane")]
    [InlineData("Öztürk", "ozturk")]
    [InlineData("Üsküdar", "uskudar")]
    [InlineData("Ahmet", "ahmet")]
    public void Normalize_Turkce_karakter_iceren_metni_aksansiz_kucuk_harf_doner(
        string input, string expected)
    {
        TurkishStringNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Normalize_null_veya_bos_metin_icin_bos_string_doner(string? input)
    {
        TurkishStringNormalizer.Normalize(input).Should().BeEmpty();
    }

    [Theory]
    [InlineData("İSTANBUL", "istanbul")]
    [InlineData("Şişli", "şişli")]
    [InlineData("ÇANKAYA", "çankaya")]
    public void TurkishLower_aksanlari_korur_yalniz_kucuk_harfe_cevirir(
        string input, string expected)
    {
        TurkishStringNormalizer.TurkishLower(input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_idempotent_dir()
    {
        const string input = "Şişli Çankaya İstanbul";
        var once = TurkishStringNormalizer.Normalize(input);
        var twice = TurkishStringNormalizer.Normalize(once);

        twice.Should().Be(once);
    }

    [Theory]
    [InlineData("Müller", "muller")]
    [InlineData("Bäcker", "backer")]
    public void Normalize_Almanca_aksanli_karakterleri_de_normalize_eder(
        string input, string expected)
    {
        TurkishStringNormalizer.Normalize(input).Should().Be(expected);
    }
}
