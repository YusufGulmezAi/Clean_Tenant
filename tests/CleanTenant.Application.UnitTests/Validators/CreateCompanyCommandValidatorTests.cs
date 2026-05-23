using CleanTenant.Application.Features.Main.Companies;
using CleanTenant.Application.UnitTests.Common;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="CreateCompanyCommandValidator"/> testleri — v0.2.13.e ile eklenen
/// zorunlu Site Yöneticisi (CompanyAdmin) alanları + mevcut Site alan kuralları
/// kapsanır. Lokalize validator için <see cref="NullStringLocalizer"/> stub kullanılır.
/// </summary>
public sealed class CreateCompanyCommandValidatorTests
{
    private readonly CreateCompanyCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static CreateCompanyCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Acme Sitesi",
        LegalName: "Acme Site Yönetimi A.Ş.",
        Vkn: "1234567890",
        Email: "site@acme.tr",
        Phone: "0532 123 45 67",
        AdminFirstName: "Yusuf",
        AdminLastName: "Gülmez",
        AdminEmail: "admin@acme.tr",
        AdminPhone: "0532 999 88 77");

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_tenant_id_hata_uretir()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.TenantId));
    }

    [Fact]
    public void Bos_isim_hata_uretir()
    {
        var cmd = ValidCommand() with { Name = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.Name));
    }

    [Fact]
    public void Bos_admin_ad_hata_uretir()
    {
        var cmd = ValidCommand() with { AdminFirstName = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.AdminFirstName));
    }

    [Fact]
    public void Bos_admin_soyad_hata_uretir()
    {
        var cmd = ValidCommand() with { AdminLastName = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.AdminLastName));
    }

    [Fact]
    public void Bos_admin_email_hata_uretir()
    {
        var cmd = ValidCommand() with { AdminEmail = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.AdminEmail));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("admin@")]
    [InlineData("@acme.tr")]
    public void Gecersiz_admin_email_hata_uretir(string email)
    {
        var cmd = ValidCommand() with { AdminEmail = email };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.AdminEmail));
    }

    [Fact]
    public void Admin_telefon_opsiyonel_null_kabul_edilir()
    {
        var cmd = ValidCommand() with { AdminPhone = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Vkn_gecersiz_format_hata_uretir()
    {
        var cmd = ValidCommand() with { Vkn = "123" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyCommand.Vkn));
    }
}
