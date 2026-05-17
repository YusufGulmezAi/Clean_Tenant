using CleanTenant.Application.Common.Auth;

namespace CleanTenant.Domain.UnitTests.SharedKernel.Auth;

public sealed class LoginIdentifierTests
{
    [Theory]
    [InlineData("yusuf@example.com", LoginIdentifierType.Email)]
    [InlineData("YUSUF@EXAMPLE.COM", LoginIdentifierType.Email)]
    [InlineData("a+b@c.co", LoginIdentifierType.Email)]
    public void Email_format_Email_olarak_tespit_edilir(string input, LoginIdentifierType expected)
    {
        var (type, _) = LoginIdentifier.Resolve(input);
        type.Should().Be(expected);
    }

    [Theory]
    [InlineData("YUSUF@EXAMPLE.COM", "yusuf@example.com")]
    [InlineData("  user@x.com  ", "user@x.com")]
    public void Email_normalize_lowercase_ve_trim_yapar(string input, string normalized)
    {
        var (_, value) = LoginIdentifier.Resolve(input);
        value.Should().Be(normalized);
    }

    [Theory]
    [InlineData("10000000146")]  // Gecerli TCKN ornegi (algorithm check geçer)
    public void Gecerli_TCKN_tespit_edilir(string input)
    {
        var (type, value) = LoginIdentifier.Resolve(input);
        type.Should().Be(LoginIdentifierType.Tckn);
        value.Should().Be(input);
    }

    [Theory]
    [InlineData("12345678901")]  // Random — checksum geçmez
    [InlineData("00000000000")]  // Sıfırlı — ilk hane 0
    [InlineData("123456")]       // 11 haneli değil
    [InlineData("abcdefghijk")]  // Rakam değil
    public void Gecersiz_TCKN_Tckn_tipinde_kabul_edilmez(string input)
    {
        var (type, _) = LoginIdentifier.Resolve(input);
        type.Should().NotBe(LoginIdentifierType.Tckn);
    }

    [Theory]
    [InlineData("+905551234567", "+905551234567")]
    [InlineData("05551234567", "+905551234567")]
    [InlineData("5551234567", "+905551234567")]
    [InlineData("905551234567", "+905551234567")]
    [InlineData("0 555 123 45 67", "+905551234567")]
    [InlineData("(0555) 123-45-67", "+905551234567")]
    public void Telefon_farkli_formatlardan_uluslararasi_format_a_normalize_edilir(string input, string normalized)
    {
        var (type, value) = LoginIdentifier.Resolve(input);
        type.Should().Be(LoginIdentifierType.PhoneNumber);
        value.Should().Be(normalized);
    }

    [Theory]
    [InlineData("12345")]              // Çok kısa
    [InlineData("4441234")]            // 5'le başlamıyor (mobil değil)
    [InlineData("invalidphone")]       // Harf
    public void Gecersiz_telefon_PhoneNumber_tipinde_kabul_edilmez(string input)
    {
        var (type, _) = LoginIdentifier.Resolve(input);
        type.Should().NotBe(LoginIdentifierType.PhoneNumber);
    }

    [Theory]
    [InlineData("99999999990")]   // YKN ornegi: 99 ile baslar, Mernis checksum'una uyar
    public void YKN_de_Tckn_tipinde_tespit_edilir(string input)
    {
        var (type, value) = LoginIdentifier.Resolve(input);
        type.Should().Be(LoginIdentifierType.Tckn,
            because: "YKN ve TCKN aynı Mernis checksum'unu kullanır; ayrı tip değildir.");
        value.Should().Be(input);
    }

    [Theory]
    [InlineData("1234567890")]   // 10 hane, ilk 0 değil → VKN
    [InlineData("9876543210")]
    public void Gecerli_VKN_tespit_edilir(string input)
    {
        var (type, value) = LoginIdentifier.Resolve(input);
        type.Should().Be(LoginIdentifierType.Vkn);
        value.Should().Be(input);
    }

    [Theory]
    [InlineData("0123456789")]   // İlk hane 0 → reddedilir
    [InlineData("123456789")]    // 9 hane → reddedilir
    [InlineData("abcdefghij")]   // Rakam değil
    public void Gecersiz_VKN_Vkn_tipinde_kabul_edilmez(string input)
    {
        var (type, _) = LoginIdentifier.Resolve(input);
        type.Should().NotBe(LoginIdentifierType.Vkn);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("???")]
    public void Bilinmeyen_format_Unknown_doner(string input)
    {
        var (type, _) = LoginIdentifier.Resolve(input);
        type.Should().Be(LoginIdentifierType.Unknown);
    }
}
