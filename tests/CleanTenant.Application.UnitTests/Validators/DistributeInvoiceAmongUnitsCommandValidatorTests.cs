using CleanTenant.Application.Features.Main.Accruals.DistributeInvoice;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Tenant.Budgeting.Enums;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="DistributeInvoiceAmongUnitsCommandValidator"/> testleri. FAZ 6 Slice 10.
/// </summary>
public sealed class DistributeInvoiceAmongUnitsCommandValidatorTests
{
    private readonly DistributeInvoiceAmongUnitsCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static DistributeInvoiceAmongUnitsCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        AccountingPeriodId: Guid.NewGuid(),
        Year: 2026,
        Month: 1,
        TotalAmount: 10_000m,
        DistributionModel: DistributionModel.BySquareMeter,
        ParticipationGroupId: null,
        ReceivableAccountCodeId: Guid.NewGuid(),
        IncomeAccountCodeId: Guid.NewGuid(),
        DueDate: new DateOnly(2026, 2, 15),
        Description: "Doğalgaz faturası Ocak 2026");

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Pozitif_olmayan_tutar_hata_uretir(decimal amount)
    {
        var result = _validator.Validate(ValidCommand() with { TotalAmount = amount });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DistributeInvoiceAmongUnitsCommand.TotalAmount));
    }

    [Fact]
    public void Bos_aciklama_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { Description = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DistributeInvoiceAmongUnitsCommand.Description));
    }

    [Fact]
    public void Bos_gelir_hesap_kodu_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { IncomeAccountCodeId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DistributeInvoiceAmongUnitsCommand.IncomeAccountCodeId));
    }
}
