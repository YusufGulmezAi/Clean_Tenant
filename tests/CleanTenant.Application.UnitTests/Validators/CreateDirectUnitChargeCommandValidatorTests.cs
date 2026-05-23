using CleanTenant.Application.Features.Main.Accruals.DirectCharge;
using CleanTenant.Application.UnitTests.Common;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="CreateDirectUnitChargeCommandValidator"/> testleri. FAZ 6 Slice 10.
/// </summary>
public sealed class CreateDirectUnitChargeCommandValidatorTests
{
    private readonly CreateDirectUnitChargeCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static CreateDirectUnitChargeCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        AccountingPeriodId: Guid.NewGuid(),
        UnitId: Guid.NewGuid(),
        Year: 2026,
        Month: 3,
        Amount: 1_500m,
        ReceivableAccountCodeId: Guid.NewGuid(),
        IncomeAccountCodeId: Guid.NewGuid(),
        DueDate: new DateOnly(2026, 4, 15),
        Description: "Depo kira Mart 2026");

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_UnitId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { UnitId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDirectUnitChargeCommand.UnitId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Pozitif_olmayan_tutar_hata_uretir(decimal amount)
    {
        var result = _validator.Validate(ValidCommand() with { Amount = amount });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDirectUnitChargeCommand.Amount));
    }

    [Fact]
    public void Bos_aciklama_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { Description = "" });
        result.IsValid.Should().BeFalse();
    }
}
