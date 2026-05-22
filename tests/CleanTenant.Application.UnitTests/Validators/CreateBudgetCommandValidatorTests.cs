using CleanTenant.Application.Features.Main.Budgeting.Budgets;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Tenant.Budgeting.Enums;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="CreateBudgetCommandValidator"/> testleri — bütçe oluşturma için
/// zorunlu alan + uzunluk kuralları. v0.2.13.a FAZ 5 Slice 4e.
/// </summary>
public sealed class CreateBudgetCommandValidatorTests
{
    private readonly CreateBudgetCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static CreateBudgetCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        FiscalYearId: Guid.NewGuid(),
        Type: BudgetType.Aidat,
        Title: "2026 Yıllık Bütçesi",
        Notes: null);

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_TenantId_hata_uretir()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBudgetCommand.TenantId));
    }

    [Fact]
    public void Bos_CompanyId_hata_uretir()
    {
        var cmd = ValidCommand() with { CompanyId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBudgetCommand.CompanyId));
    }

    [Fact]
    public void Bos_FiscalYearId_hata_uretir()
    {
        var cmd = ValidCommand() with { FiscalYearId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBudgetCommand.FiscalYearId));
    }

    [Fact]
    public void Bos_Title_hata_uretir()
    {
        var cmd = ValidCommand() with { Title = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBudgetCommand.Title));
    }

    [Fact]
    public void Uzun_Title_hata_uretir()
    {
        var cmd = ValidCommand() with { Title = new string('A', 121) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBudgetCommand.Title));
    }

    [Fact]
    public void Uzun_Notes_hata_uretir()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 2001) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBudgetCommand.Notes));
    }

    [Fact]
    public void Null_Notes_kabul_edilir()
    {
        var cmd = ValidCommand() with { Notes = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
