using CleanTenant.Application.Features.Main.Budgeting.Budgets;
using CleanTenant.Application.UnitTests.Common;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="PublishBudgetCommandValidator"/> testleri. v0.2.13.a FAZ 5 Slice 4e.
/// </summary>
public sealed class PublishBudgetCommandValidatorTests
{
    private readonly PublishBudgetCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static PublishBudgetCommand ValidCommand() => new(
        CompanyId: Guid.NewGuid(),
        BudgetId: Guid.NewGuid(),
        ValidFrom: new DateOnly(2026, 1, 1));

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_CompanyId_hata_uretir()
    {
        var cmd = ValidCommand() with { CompanyId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PublishBudgetCommand.CompanyId));
    }

    [Fact]
    public void Bos_BudgetId_hata_uretir()
    {
        var cmd = ValidCommand() with { BudgetId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PublishBudgetCommand.BudgetId));
    }

    [Fact]
    public void Bos_ValidFrom_hata_uretir()
    {
        var cmd = ValidCommand() with { ValidFrom = default };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PublishBudgetCommand.ValidFrom));
    }
}
