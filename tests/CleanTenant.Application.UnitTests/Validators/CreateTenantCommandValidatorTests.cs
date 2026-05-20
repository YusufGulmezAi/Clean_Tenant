using CleanTenant.Application.Features.Catalog.Tenants;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Identity.Tenants;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="CreateTenantCommandValidator"/> testleri — VKN/TCKN/YKN
/// koşullu format kuralları, Sorumlu Yönetici alanları, telefon mask
/// validasyonu kapsanır. v0.2.11.d — lokalize validator için
/// <see cref="NullStringLocalizer"/> stub kullanılır.
/// </summary>
public sealed class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static CreateTenantCommand ValidVknCommand() => new(
        Name: "Acme Sites Ltd.",
        LegalName: "Acme Site Yönetim Limited",
        LegalIdentityType: LegalIdentityType.Vkn,
        LegalIdentityNumber: "1234567890",
        Address: "İstanbul",
        BillingTier: BillingTier.Standard,
        HasDedicatedDatabase: false,
        AdminFirstName: "Yusuf",
        AdminLastName: "Gülmez",
        AdminEmail: "admin@acme.tr",
        AdminPhone: "0(532) 123-45-67");

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        var result = _validator.Validate(ValidVknCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_isim_hata_uretir()
    {
        var cmd = ValidVknCommand() with { Name = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantCommand.Name));
    }

    [Theory]
    [InlineData("1234567890")]   // 10 hane, ilk hane 1-9
    [InlineData("9876543210")]
    public void Vkn_format_dogru_kabul_eder(string vkn)
    {
        var cmd = ValidVknCommand() with { LegalIdentityNumber = vkn };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("0234567890")]   // ilk hane 0
    [InlineData("123456789")]    // 9 hane
    [InlineData("12345678901")]  // 11 hane (TCKN/YKN uzunluğu)
    [InlineData("123abc7890")]   // harf
    public void Vkn_format_yanlis_hata_uretir(string vkn)
    {
        var cmd = ValidVknCommand() with { LegalIdentityNumber = vkn };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantCommand.LegalIdentityNumber));
    }

    [Theory]
    [InlineData("12345678901")]  // 11 hane, ilk hane 1-9
    [InlineData("98765432109")]
    public void Tckn_format_dogru_kabul_eder(string tckn)
    {
        var cmd = ValidVknCommand() with
        {
            LegalIdentityType = LegalIdentityType.Tckn,
            LegalIdentityNumber = tckn,
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("01234567890")]  // ilk hane 0
    [InlineData("1234567890")]   // 10 hane (VKN uzunluğu)
    public void Tckn_format_yanlis_hata_uretir(string tckn)
    {
        var cmd = ValidVknCommand() with
        {
            LegalIdentityType = LegalIdentityType.Tckn,
            LegalIdentityNumber = tckn,
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("99123456789")]  // '99' ile başlar, 11 hane
    [InlineData("99000000000")]
    public void Ykn_format_dogru_kabul_eder(string ykn)
    {
        var cmd = ValidVknCommand() with
        {
            LegalIdentityType = LegalIdentityType.Ykn,
            LegalIdentityNumber = ykn,
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345678901")]  // '99' ile başlamıyor
    [InlineData("9912345678")]   // 10 hane
    public void Ykn_format_yanlis_hata_uretir(string ykn)
    {
        var cmd = ValidVknCommand() with
        {
            LegalIdentityType = LegalIdentityType.Ykn,
            LegalIdentityNumber = ykn,
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Bos_admin_email_hata_uretir()
    {
        var cmd = ValidVknCommand() with { AdminEmail = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantCommand.AdminEmail));
    }

    [Fact]
    public void Gecersiz_admin_email_hata_uretir()
    {
        var cmd = ValidVknCommand() with { AdminEmail = "not-an-email" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("0(532) 123-45-67")]
    [InlineData("0(555) 999-99-99")]
    public void Telefon_format_dogru_kabul_eder(string phone)
    {
        var cmd = ValidVknCommand() with { AdminPhone = phone };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("05321234567")]        // boşluk/parantez yok
    [InlineData("(532) 123-45-67")]    // baştaki 0 eksik
    [InlineData("0(432) 123-45-67")]   // 5 ile başlamıyor
    [InlineData("0(532) 123 45 67")]   // tire yerine boşluk
    public void Telefon_format_yanlis_hata_uretir(string phone)
    {
        var cmd = ValidVknCommand() with { AdminPhone = phone };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantCommand.AdminPhone));
    }
}
