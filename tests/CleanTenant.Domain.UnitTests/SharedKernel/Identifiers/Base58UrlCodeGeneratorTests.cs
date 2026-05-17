using CleanTenant.SharedKernel.Identifiers;

namespace CleanTenant.Domain.UnitTests.SharedKernel.Identifiers;

public sealed class Base58UrlCodeGeneratorTests
{
    private const string Base58Alphabet =
        "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    [Fact]
    public void Generate_dokuz_karakterli_kod_uretir()
    {
        var generator = new Base58UrlCodeGenerator();

        var code = generator.Generate();

        code.Should().HaveLength(9);
    }

    [Fact]
    public void Generate_yalniz_Base58_alfabesinden_karakter_iceri()
    {
        var generator = new Base58UrlCodeGenerator();

        var code = generator.Generate();

        // Regex: tam 9 karakter, sadece Base58 alfabesi (0/O/I/l harici).
        code.Should().MatchRegex("^[1-9A-HJ-NP-Za-km-z]{9}$");
    }

    [Fact]
    public void Generate_yasakli_karakter_icermez()
    {
        var generator = new Base58UrlCodeGenerator();

        // 100 kod uretip 0/O/I/l karakterlerinin hicbirinin gecmedigini dogrula
        for (var i = 0; i < 100; i++)
        {
            var code = generator.Generate();
            code.Should().NotContainAny("0", "O", "I", "l");
        }
    }

    [Fact]
    public void Generate_bin_uretimde_dramatik_oranda_benzersizdir()
    {
        var generator = new Base58UrlCodeGenerator();
        var codes = new HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            codes.Add(generator.Generate());
        }

        // 58^9 ~= 1.85e15 kombinasyon; 1000 deneme icinde carpisma matematiksel olarak ihmal edilebilir
        codes.Should().HaveCount(1000);
    }
}
