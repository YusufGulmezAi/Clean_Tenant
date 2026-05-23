using CleanTenant.Application.Features.Main.LateFees.SetLateFeePolicy;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Tenant.LateFees;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="SetLateFeePolicyCommandValidator"/> testleri. FAZ 7B Slice 6.
/// </summary>
public sealed class SetLateFeePolicyCommandValidatorTests
{
    private readonly SetLateFeePolicyCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static SetLateFeePolicyCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        BudgetId: null,
        MonthlyRatePercent: 3m,
        IsCompound: false,
        GraceDays: 5,
        IncomeAccountCodeId: Guid.NewGuid());

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void KMK_tavanina_esit_oran_kabul_edilir()
    {
        var cmd = ValidCommand() with { MonthlyRatePercent = RegulatoryLimits.KmkM20MonthlyCapPercent };
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Pozitif_olmayan_oran_hata_uretir(decimal rate)
    {
        var result = _validator.Validate(ValidCommand() with { MonthlyRatePercent = rate });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetLateFeePolicyCommand.MonthlyRatePercent));
    }

    [Fact]
    public void KMK_tavanini_asan_oran_hata_uretir()
    {
        var cmd = ValidCommand() with { MonthlyRatePercent = RegulatoryLimits.KmkM20MonthlyCapPercent + 0.01m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetLateFeePolicyCommand.MonthlyRatePercent));
    }

    [Fact]
    public void Negatif_grace_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { GraceDays = -1 });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetLateFeePolicyCommand.GraceDays));
    }

    [Fact]
    public void Bos_IncomeAccountCodeId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { IncomeAccountCodeId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetLateFeePolicyCommand.IncomeAccountCodeId));
    }

    [Fact]
    public void Bos_CompanyId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { CompanyId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetLateFeePolicyCommand.CompanyId));
    }
}
