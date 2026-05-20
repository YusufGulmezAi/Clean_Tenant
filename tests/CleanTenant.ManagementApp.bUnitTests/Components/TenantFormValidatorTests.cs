using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.ManagementApp.Components.Shared;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary>
/// <see cref="TenantFormValidator"/> mod-bağımlı kural davranışı testleri.
/// Sunucu validator'larıyla aynı kuralları içerir; UI önce kullanıcıya
/// erken geri bildirim verir. v0.2.11.d — lokalize validator için
/// <see cref="NullStringLocalizer"/> stub kullanılır.
/// </summary>
public sealed class TenantFormValidatorTests
{
    private static TenantFormModel ValidModel() => new()
    {
        Name = "Acme Sites Ltd.",
        LegalName = "Acme Site Yönetim Limited",
        LegalIdentityType = LegalIdentityType.Vkn,
        LegalIdentityNumber = "1234567890",
        Address = "İstanbul",
        BillingTier = BillingTier.Standard,
        AdminFirstName = "Yusuf",
        AdminLastName = "Gülmez",
        AdminEmail = "admin@acme.tr",
        AdminPhone = "0(532) 123-45-67",
    };

    [Fact]
    public void Create_modunda_gecerli_model_validation_basarili()
    {
        var validator = new TenantFormValidator(TenantFormMode.Create, NullStringLocalizer.Instance);
        var result = validator.Validate(ValidModel());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Edit_modunda_admin_alanlari_bos_olabilir()
    {
        var validator = new TenantFormValidator(TenantFormMode.Edit, NullStringLocalizer.Instance);
        var model = ValidModel();
        model.AdminFirstName = string.Empty;
        model.AdminEmail = string.Empty;
        model.AdminPhone = string.Empty;

        var result = validator.Validate(model);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Settings_modunda_kimlik_alanlari_bos_olsa_da_gecerli()
    {
        var validator = new TenantFormValidator(TenantFormMode.Settings, NullStringLocalizer.Instance);
        var model = ValidModel();
        model.LegalIdentityNumber = string.Empty;

        var result = validator.Validate(model);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_modunda_admin_email_bos_hata_uretir()
    {
        var validator = new TenantFormValidator(TenantFormMode.Create, NullStringLocalizer.Instance);
        var model = ValidModel();
        model.AdminEmail = string.Empty;

        var result = validator.Validate(model);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TenantFormModel.AdminEmail));
    }

    [Theory]
    [InlineData(LegalIdentityType.Vkn, "1234567890", true)]
    [InlineData(LegalIdentityType.Vkn, "12345678901", false)]
    [InlineData(LegalIdentityType.Tckn, "12345678901", true)]
    [InlineData(LegalIdentityType.Tckn, "1234567890", false)]
    [InlineData(LegalIdentityType.Ykn, "99123456789", true)]
    [InlineData(LegalIdentityType.Ykn, "98123456789", false)]
    public void Edit_modunda_kimlik_format_tipe_gore_kontrol(LegalIdentityType type, string number, bool expectValid)
    {
        var validator = new TenantFormValidator(TenantFormMode.Edit, NullStringLocalizer.Instance);
        var model = ValidModel();
        model.LegalIdentityType = type;
        model.LegalIdentityNumber = number;

        var result = validator.Validate(model);
        result.IsValid.Should().Be(expectValid);
    }

    [Fact]
    public void Bos_isim_her_modda_hata_uretir()
    {
        foreach (var mode in new[] { TenantFormMode.Create, TenantFormMode.Edit, TenantFormMode.Settings })
        {
            var validator = new TenantFormValidator(mode, NullStringLocalizer.Instance);
            var model = ValidModel();
            model.Name = string.Empty;

            var result = validator.Validate(model);
            result.IsValid.Should().BeFalse($"Mode={mode}");
        }
    }
}
