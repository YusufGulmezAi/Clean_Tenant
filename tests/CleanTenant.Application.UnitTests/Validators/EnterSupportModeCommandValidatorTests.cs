using CleanTenant.Application.Features.System.EnterSupportMode;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary><see cref="EnterSupportModeCommandValidator"/> testleri.</summary>
public sealed class EnterSupportModeCommandValidatorTests
{
    private readonly EnterSupportModeCommandValidator _validator = new();

    [Fact]
    public void Empty_tenant_id_hata_uretmeli()
    {
        var cmd = new EnterSupportModeCommand(Guid.Empty, "Yeterince uzun sebep — destek erişimi", "127.0.0.1", "ua");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SUP-001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("kısa")]
    [InlineData("on dokuz karakter11")] // 20 karakterden az
    public void Kisa_sebep_hata_uretmeli(string reason)
    {
        var cmd = new EnterSupportModeCommand(Guid.NewGuid(), reason, "127.0.0.1", "ua");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SUP-001");
    }

    [Fact]
    public void Gecerli_tenant_id_ve_uzun_sebep_dogrulanmali()
    {
        var cmd = new EnterSupportModeCommand(
            Guid.NewGuid(),
            "Müşteri talebi destek erişimi — TKT-42",
            "127.0.0.1",
            "ua");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }
}
