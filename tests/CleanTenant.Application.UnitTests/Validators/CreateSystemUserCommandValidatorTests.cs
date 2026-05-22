using CleanTenant.Application.Features.System.Users;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="CreateSystemUserCommandValidator"/> testleri — zorunlu alanlar,
/// e-posta format, Türkiye cep telefonu format validasyonu.
/// </summary>
public sealed class CreateSystemUserCommandValidatorTests
{
    private readonly CreateSystemUserCommandValidator _validator = new();

    private static CreateSystemUserCommand ValidCommand() => new(
        FirstName: "Ayşe",
        LastName: "Yılmaz",
        Email: "ayse@example.com",
        PhoneNumber: "05321234567",
        Password: "Secure123",
        RoleIds: [Guid.NewGuid()]);

    [Fact]
    public void Tum_alanlar_dogru_gecerli_olmali()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Telefon_null_gecerli_olmali()
    {
        var cmd = ValidCommand() with { PhoneNumber = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Telefon_bos_string_gecerli_olmali()
    {
        var cmd = ValidCommand() with { PhoneNumber = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    // ─── Ad / Soyad ───────────────────────────────────────────────────────

    [Fact]
    public void Bos_ad_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { FirstName = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSystemUserCommand.FirstName));
    }

    [Fact]
    public void Bos_soyad_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { LastName = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSystemUserCommand.LastName));
    }

    [Fact]
    public void Cok_uzun_ad_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { FirstName = new string('A', 101) });
        result.IsValid.Should().BeFalse();
    }

    // ─── E-posta ───────────────────────────────────────────────────────────

    [Fact]
    public void Bos_email_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { Email = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSystemUserCommand.Email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@no-local-part.com")]
    public void Gecersiz_email_format_hata_uretmeli(string email)
    {
        var result = _validator.Validate(ValidCommand() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSystemUserCommand.Email));
    }

    // ─── Telefon ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("05321234567")]
    [InlineData("5321234567")]
    [InlineData("+905321234567")]
    [InlineData("905321234567")]
    [InlineData("0(532) 123-45-67")]
    [InlineData("+90 532 123 45 67")]
    public void Gecerli_turkiye_cep_telefonu_kabul_edilmeli(string phone)
    {
        var result = _validator.Validate(ValidCommand() with { PhoneNumber = phone });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("1234567890")]          // 5 ile başlamıyor
    [InlineData("0432 123 45 67")]      // sabit hat formatı, 5 ile başlamıyor
    [InlineData("123")]                 // çok kısa
    [InlineData("abcdefghij")]          // harf
    [InlineData("0532 123 45 6")]       // eksik hane
    public void Gecersiz_telefon_format_hata_uretmeli(string phone)
    {
        var result = _validator.Validate(ValidCommand() with { PhoneNumber = phone });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSystemUserCommand.PhoneNumber));
    }

    // ─── Şifre ─────────────────────────────────────────────────────────────

    [Fact]
    public void Bos_sifre_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { Password = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSystemUserCommand.Password));
    }

    [Fact]
    public void Kisa_sifre_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { Password = "1234567" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSystemUserCommand.Password));
    }

    [Fact]
    public void Sekiz_karakter_sifre_gecerli_olmali()
    {
        var result = _validator.Validate(ValidCommand() with { Password = "12345678" });
        result.IsValid.Should().BeTrue();
    }

    // ─── Roller ────────────────────────────────────────────────────────────

    [Fact]
    public void Bos_rol_listesi_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { RoleIds = [] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSystemUserCommand.RoleIds));
    }
}
