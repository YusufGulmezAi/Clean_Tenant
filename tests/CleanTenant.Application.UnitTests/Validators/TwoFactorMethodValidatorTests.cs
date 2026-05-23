using CleanTenant.Application.Features.Auth.TwoFactor.EmailMethod;
using CleanTenant.Application.Features.Auth.TwoFactor.PhoneMethod;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// v0.2.13 — Profil 2FA self-servis doğrulama komutlarının validator testleri:
/// e-posta kodu, telefon numarası ve telefon kodu validasyonu.
/// </summary>
public sealed class TwoFactorMethodValidatorTests
{
    private readonly ConfirmEmailVerificationCommandValidator _emailValidator = new();
    private readonly SendPhoneVerificationCodeCommandValidator _sendPhoneValidator = new();
    private readonly ConfirmPhoneVerificationCommandValidator _confirmPhoneValidator = new();

    // ─── E-posta kodu ───────────────────────────────────────────────────────

    [Fact]
    public void Email_dolu_kod_gecerli_olmali()
    {
        var result = _emailValidator.Validate(new ConfirmEmailVerificationCommand("123456"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Email_bos_kod_hata_uretmeli()
    {
        var result = _emailValidator.Validate(new ConfirmEmailVerificationCommand(string.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ─── Telefon kod gönderme ────────────────────────────────────────────────

    [Theory]
    [InlineData("05321234567")]
    [InlineData("5321234567")]
    [InlineData("+905321234567")]
    public void SendPhone_gecerli_numara_kabul_edilmeli(string phone)
    {
        var result = _sendPhoneValidator.Validate(new SendPhoneVerificationCodeCommand(phone));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567890")]   // 5 ile başlamıyor
    [InlineData("abcdefghij")]
    public void SendPhone_gecersiz_numara_hata_uretmeli(string phone)
    {
        var result = _sendPhoneValidator.Validate(new SendPhoneVerificationCodeCommand(phone));
        result.IsValid.Should().BeFalse();
    }

    // ─── Telefon kod doğrulama ──────────────────────────────────────────────

    [Fact]
    public void ConfirmPhone_gecerli_numara_ve_kod_gecerli_olmali()
    {
        var result = _confirmPhoneValidator.Validate(
            new ConfirmPhoneVerificationCommand("05321234567", "123456"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConfirmPhone_bos_kod_hata_uretmeli()
    {
        var result = _confirmPhoneValidator.Validate(
            new ConfirmPhoneVerificationCommand("05321234567", string.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConfirmPhone_gecersiz_numara_hata_uretmeli()
    {
        var result = _confirmPhoneValidator.Validate(
            new ConfirmPhoneVerificationCommand("123", "123456"));
        result.IsValid.Should().BeFalse();
    }
}
