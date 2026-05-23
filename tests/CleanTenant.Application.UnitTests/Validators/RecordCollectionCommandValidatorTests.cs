using CleanTenant.Application.Features.Main.Collections.RecordCollection;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Tenant.Collections.Enums;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="RecordCollectionCommandValidator"/> testleri. FAZ 7 Slice 6.
/// </summary>
public sealed class RecordCollectionCommandValidatorTests
{
    private readonly RecordCollectionCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static RecordCollectionCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        UnitId: Guid.NewGuid(),
        AccountingPeriodId: Guid.NewGuid(),
        PaymentDate: new DateOnly(2026, 3, 15),
        Amount: 1_500m,
        Method: PaymentMethod.Cash,
        CashAccountCodeId: Guid.NewGuid(),
        Reference: "DEKONT-2026-001",
        Description: "Mart 2026 aidat ödemesi");

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Reference_ve_Description_null_validation_basarili()
    {
        var cmd = ValidCommand() with { Reference = null, Description = null };
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_UnitId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { UnitId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordCollectionCommand.UnitId));
    }

    [Fact]
    public void Bos_CompanyId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { CompanyId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordCollectionCommand.CompanyId));
    }

    [Fact]
    public void Bos_AccountingPeriodId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { AccountingPeriodId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordCollectionCommand.AccountingPeriodId));
    }

    [Fact]
    public void Bos_CashAccountCodeId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { CashAccountCodeId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordCollectionCommand.CashAccountCodeId));
    }

    [Fact]
    public void Bos_PaymentDate_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { PaymentDate = default });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordCollectionCommand.PaymentDate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1500)]
    public void Pozitif_olmayan_tutar_hata_uretir(decimal amount)
    {
        var result = _validator.Validate(ValidCommand() with { Amount = amount });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordCollectionCommand.Amount));
    }

    [Fact]
    public void Cok_uzun_Reference_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { Reference = new string('x', 201) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordCollectionCommand.Reference));
    }

    [Fact]
    public void Cok_uzun_Description_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { Description = new string('x', 501) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordCollectionCommand.Description));
    }
}
