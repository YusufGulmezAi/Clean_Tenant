using CleanTenant.Application.Features.System.Users;
using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="UpdateUserCommandValidator"/> testleri — zorunlu alanlar,
/// e-posta ve telefon format validasyonu.
/// </summary>
public sealed class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    private static UpdateUserCommand ValidCommand() => new(
        UrlCode: "abc123xyz",
        FirstName: "Mehmet",
        LastName: "Demir",
        Email: "mehmet@example.com",
        PhoneNumber: "05551234567",
        Scope: ScopeLevel.System,
        TenantId: null,
        CompanyId: null,
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

    // ─── UrlCode ───────────────────────────────────────────────────────────

    [Fact]
    public void Bos_urlcode_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { UrlCode = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.UrlCode));
    }

    // ─── Ad / Soyad ───────────────────────────────────────────────────────

    [Fact]
    public void Bos_ad_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { FirstName = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.FirstName));
    }

    [Fact]
    public void Bos_soyad_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { LastName = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.LastName));
    }

    // ─── E-posta ───────────────────────────────────────────────────────────

    [Fact]
    public void Bos_email_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { Email = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.Email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@domain.com")]
    [InlineData("no-domain")]
    public void Gecersiz_email_format_hata_uretmeli(string email)
    {
        var result = _validator.Validate(ValidCommand() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.Email));
    }

    // ─── Telefon ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("05321234567")]
    [InlineData("5321234567")]
    [InlineData("+905321234567")]
    [InlineData("0(532) 123-45-67")]
    public void Gecerli_telefon_formatlari_kabul_edilmeli(string phone)
    {
        var result = _validator.Validate(ValidCommand() with { PhoneNumber = phone });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("0432 123 45 67")]
    public void Gecersiz_telefon_formatlari_hata_uretmeli(string phone)
    {
        var result = _validator.Validate(ValidCommand() with { PhoneNumber = phone });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.PhoneNumber));
    }

    // ─── Roller ────────────────────────────────────────────────────────────

    [Fact]
    public void Bos_rol_listesi_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { RoleIds = [] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.RoleIds));
    }
}
