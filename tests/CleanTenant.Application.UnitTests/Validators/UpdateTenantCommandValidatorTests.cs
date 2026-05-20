using CleanTenant.Application.Features.Catalog.Tenants;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Identity.Tenants;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="UpdateTenantCommandValidator"/> testleri — Create ile aynı format
/// kuralları + Sorumlu Yönetici alanlarının olmaması, TenantId zorunluluğu.
/// v0.2.11.d — lokalize validator için <see cref="NullStringLocalizer"/> stub.
/// </summary>
public sealed class UpdateTenantCommandValidatorTests
{
    private readonly UpdateTenantCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static UpdateTenantCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Acme Sites Ltd.",
        LegalName: "Acme Site Yönetim Limited",
        LegalIdentityType: LegalIdentityType.Vkn,
        LegalIdentityNumber: "1234567890",
        Address: "İstanbul",
        BillingTier: BillingTier.Standard,
        HasDedicatedDatabase: false,
        AllowSystemWriteAccess: true);

    [Fact]
    public void Gecerli_komut_validation_basarili()
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTenantCommand.TenantId));
    }

    [Fact]
    public void Bos_isim_hata_uretir()
    {
        var cmd = ValidCommand() with { Name = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTenantCommand.Name));
    }

    [Fact]
    public void Cok_uzun_isim_hata_uretir()
    {
        var cmd = ValidCommand() with { Name = new string('A', 257) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(LegalIdentityType.Vkn, "1234567890", true)]
    [InlineData(LegalIdentityType.Vkn, "0234567890", false)]
    [InlineData(LegalIdentityType.Tckn, "12345678901", true)]
    [InlineData(LegalIdentityType.Tckn, "1234567890", false)]
    [InlineData(LegalIdentityType.Ykn, "99123456789", true)]
    [InlineData(LegalIdentityType.Ykn, "12345678901", false)]
    public void Kimlik_tipi_ve_format_eslesmesi(LegalIdentityType type, string number, bool expectValid)
    {
        var cmd = ValidCommand() with
        {
            LegalIdentityType = type,
            LegalIdentityNumber = number,
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().Be(expectValid);
    }
}
