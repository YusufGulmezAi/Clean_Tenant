using CleanTenant.Application.Features.Main.Budgeting.Budgets;
using CleanTenant.Application.UnitTests.Common;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="CloneBudgetCommandValidator"/> testleri (FAZ A — bütçe yenileme).
/// </summary>
public sealed class CloneBudgetCommandValidatorTests
{
    private readonly CloneBudgetCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static CloneBudgetCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        SourceBudgetId: Guid.NewGuid(),
        NewFiscalYearId: Guid.NewGuid(),
        NewTitle: "2027 Yıllık Aidat");

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_SourceBudgetId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { SourceBudgetId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CloneBudgetCommand.SourceBudgetId));
    }

    [Fact]
    public void Bos_NewFiscalYearId_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { NewFiscalYearId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CloneBudgetCommand.NewFiscalYearId));
    }

    [Fact]
    public void Bos_NewTitle_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { NewTitle = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CloneBudgetCommand.NewTitle));
    }

    [Fact]
    public void Cok_uzun_NewTitle_hata_uretir()
    {
        var result = _validator.Validate(ValidCommand() with { NewTitle = new string('x', 121) });
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Gecersiz_PeriodStartMonth_hata_uretir(int month)
    {
        var result = _validator.Validate(ValidCommand() with { PeriodStartYear = 2027, PeriodStartMonth = month });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Eksik_period_cifti_hata_uretir()
    {
        // Başlangıç yılı var ama ayı yok → tutarsız
        var result = _validator.Validate(ValidCommand() with { PeriodStartYear = 2027, PeriodStartMonth = null });
        result.IsValid.Should().BeFalse();
    }
}
