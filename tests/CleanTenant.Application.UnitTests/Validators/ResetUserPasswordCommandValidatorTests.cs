using CleanTenant.Application.Features.System.Users;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="ResetUserPasswordCommandValidator"/> testleri — urlCode zorunlu,
/// yeni şifre minimum uzunluk.
/// </summary>
public sealed class ResetUserPasswordCommandValidatorTests
{
    private readonly ResetUserPasswordCommandValidator _validator = new();

    private static ResetUserPasswordCommand ValidCommand() => new(
        UrlCode: "abc123xyz",
        NewPassword: "NewPass99");

    [Fact]
    public void Tum_alanlar_dogru_gecerli_olmali()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    // ─── UrlCode ───────────────────────────────────────────────────────────

    [Fact]
    public void Bos_urlcode_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { UrlCode = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetUserPasswordCommand.UrlCode));
    }

    // ─── Yeni Şifre ────────────────────────────────────────────────────────

    [Fact]
    public void Bos_yeni_sifre_hata_uretmeli()
    {
        var result = _validator.Validate(ValidCommand() with { NewPassword = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetUserPasswordCommand.NewPassword));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1234567")]
    public void Yedi_veya_daha_az_karakter_hata_uretmeli(string password)
    {
        var result = _validator.Validate(ValidCommand() with { NewPassword = password });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetUserPasswordCommand.NewPassword));
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("VeryLongPassword123!")]
    public void Sekiz_veya_daha_fazla_karakter_gecerli_olmali(string password)
    {
        var result = _validator.Validate(ValidCommand() with { NewPassword = password });
        result.IsValid.Should().BeTrue();
    }
}
