using CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;
using CleanTenant.Application.UnitTests.Common;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="GenerateBudgetAccrualCommandValidator"/> testleri. FAZ 6 Slice 10.
/// </summary>
public sealed class GenerateBudgetAccrualCommandValidatorTests
{
    private readonly GenerateBudgetAccrualCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static GenerateBudgetAccrualCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        BudgetId: Guid.NewGuid(),
        Year: 2026,
        Month: 2);

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_BudgetId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { BudgetId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateBudgetAccrualCommand.BudgetId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Gecersiz_ay_hata_uretir(int month)
    {
        var result = _validator.Validate(ValidCommand() with { Month = month });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateBudgetAccrualCommand.Month));
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Gecersiz_yil_hata_uretir(int year)
    {
        var result = _validator.Validate(ValidCommand() with { Year = year });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateBudgetAccrualCommand.Year));
    }
}
