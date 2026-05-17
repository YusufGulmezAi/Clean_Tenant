using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="LoginCommandValidator"/> testleri. Inline validation v0.1.6'da
/// FluentValidation'a taşındı; bu testler kural davranışını doğrular.
/// </summary>
public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Bos_identifier_hata_uretmeli()
    {
        var cmd = new LoginCommand("", "TestPass-2026!", PersonaSide.Management, null, "127.0.0.1", "ua");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AUTH-001");
    }

    [Fact]
    public void Bos_password_hata_uretmeli()
    {
        var cmd = new LoginCommand("user@x.com", "", PersonaSide.Management, null, "127.0.0.1", "ua");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AUTH-001");
    }

    [Fact]
    public void Hem_identifier_hem_password_bos_iki_hata_uretmeli()
    {
        var cmd = new LoginCommand("", "", PersonaSide.Management, null, "127.0.0.1", "ua");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Tum_alanlar_dolu_gecerli_olmali()
    {
        var cmd = new LoginCommand("user@x.com", "TestPass-2026!", PersonaSide.Management, null, "127.0.0.1", "ua");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }
}
