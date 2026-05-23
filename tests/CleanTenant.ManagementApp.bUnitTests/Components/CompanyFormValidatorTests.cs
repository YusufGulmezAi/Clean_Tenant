using CleanTenant.ManagementApp.Components.Shared;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary>
/// <see cref="CompanyFormValidator"/> mod-bağımlı kural testleri (v0.2.13.e).
/// Site Yöneticisi (CompanyAdmin) alanları yalnız Create modunda zorunludur;
/// Edit modunda boş olabilir. Lokalize validator için <see cref="NullStringLocalizer"/>.
/// </summary>
public sealed class CompanyFormValidatorTests
{
    private static CompanyFormModel ValidCreateModel() => new()
    {
        Name = "Acme Sitesi",
        LegalName = "Acme Site Yönetimi A.Ş.",
        Vkn = "1234567890",
        Email = "site@acme.tr",
        Phone = "0532 123 45 67",
        AdminFirstName = "Yusuf",
        AdminLastName = "Gülmez",
        AdminEmail = "admin@acme.tr",
        AdminPhone = "0532 999 88 77",
    };

    [Fact]
    public void Create_modunda_gecerli_model_basarili()
    {
        var validator = new CompanyFormValidator(CompanyFormMode.Create, NullStringLocalizer.Instance);
        var result = validator.Validate(ValidCreateModel());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_modunda_admin_email_bos_hata_uretir()
    {
        var validator = new CompanyFormValidator(CompanyFormMode.Create, NullStringLocalizer.Instance);
        var model = ValidCreateModel();
        model.AdminEmail = string.Empty;

        var result = validator.Validate(model);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CompanyFormModel.AdminEmail));
    }

    [Fact]
    public void Create_modunda_admin_ad_soyad_bos_hata_uretir()
    {
        var validator = new CompanyFormValidator(CompanyFormMode.Create, NullStringLocalizer.Instance);
        var model = ValidCreateModel();
        model.AdminFirstName = string.Empty;
        model.AdminLastName = string.Empty;

        var result = validator.Validate(model);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CompanyFormModel.AdminFirstName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CompanyFormModel.AdminLastName));
    }

    [Fact]
    public void Edit_modunda_admin_alanlari_bos_olabilir()
    {
        var validator = new CompanyFormValidator(CompanyFormMode.Edit, NullStringLocalizer.Instance);
        var model = ValidCreateModel();
        model.AdminFirstName = string.Empty;
        model.AdminLastName = string.Empty;
        model.AdminEmail = string.Empty;
        model.AdminPhone = null;

        var result = validator.Validate(model);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Site_adi_her_iki_modda_da_zorunlu()
    {
        var createValidator = new CompanyFormValidator(CompanyFormMode.Create, NullStringLocalizer.Instance);
        var editValidator = new CompanyFormValidator(CompanyFormMode.Edit, NullStringLocalizer.Instance);

        var model = ValidCreateModel();
        model.Name = string.Empty;

        createValidator.Validate(model).IsValid.Should().BeFalse();
        editValidator.Validate(model).IsValid.Should().BeFalse();
    }
}
