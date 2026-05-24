using CleanTenant.Application.Features.Main.Budgeting.Templates;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Budgeting;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// FAZ B — bütçe şablonu komutlarının validator testleri.
/// </summary>
public sealed class BudgetTemplateCommandValidatorTests
{
    private readonly SaveBudgetAsTemplateCommandValidator _save = new(NullStringLocalizer.Instance);
    private readonly CreateBudgetFromTemplateCommandValidator _create = new(NullStringLocalizer.Instance);

    private static SaveBudgetAsTemplateCommand ValidSave() => new(
        TenantId: Guid.NewGuid(), CompanyId: Guid.NewGuid(), SourceBudgetId: Guid.NewGuid(),
        TemplateName: "Aidat Şablonu", Description: null, Visibility: TemplateVisibility.Public);

    private static CreateBudgetFromTemplateCommand ValidCreate() => new(
        TenantId: Guid.NewGuid(), CompanyId: Guid.NewGuid(), TemplateId: Guid.NewGuid(),
        FiscalYearId: Guid.NewGuid(), Title: "2026 Aidat");

    [Fact]
    public void Save_tum_alanlar_dogru_basarili()
        => _save.Validate(ValidSave()).IsValid.Should().BeTrue();

    [Fact]
    public void Save_bos_SourceBudgetId_hata()
        => _save.Validate(ValidSave() with { SourceBudgetId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Save_bos_TemplateName_hata()
        => _save.Validate(ValidSave() with { TemplateName = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void Save_gecersiz_Visibility_hata()
        => _save.Validate(ValidSave() with { Visibility = (TemplateVisibility)99 }).IsValid.Should().BeFalse();

    [Fact]
    public void Create_tum_alanlar_dogru_basarili()
        => _create.Validate(ValidCreate()).IsValid.Should().BeTrue();

    [Fact]
    public void Create_bos_TemplateId_hata()
        => _create.Validate(ValidCreate() with { TemplateId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Create_bos_Title_hata()
        => _create.Validate(ValidCreate() with { Title = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void Create_eksik_period_cifti_hata()
        => _create.Validate(ValidCreate() with { PeriodStartYear = 2026, PeriodStartMonth = null })
            .IsValid.Should().BeFalse();
}
