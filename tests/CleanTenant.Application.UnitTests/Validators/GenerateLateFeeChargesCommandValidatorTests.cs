using CleanTenant.Application.Features.Main.LateFees.GenerateLateFeeCharges;
using CleanTenant.Application.UnitTests.Common;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="GenerateLateFeeChargesCommandValidator"/> testleri. FAZ 7B Slice 6.
/// </summary>
public sealed class GenerateLateFeeChargesCommandValidatorTests
{
    private readonly GenerateLateFeeChargesCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static GenerateLateFeeChargesCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        AsOfDate: new DateOnly(2026, 3, 31));

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_TenantId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { TenantId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateLateFeeChargesCommand.TenantId));
    }

    [Fact]
    public void Bos_CompanyId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { CompanyId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateLateFeeChargesCommand.CompanyId));
    }

    [Fact]
    public void Bos_AsOfDate_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { AsOfDate = default });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateLateFeeChargesCommand.AsOfDate));
    }
}
