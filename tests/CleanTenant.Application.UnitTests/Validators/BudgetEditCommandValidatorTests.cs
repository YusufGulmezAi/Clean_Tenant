using CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;
using CleanTenant.Application.Features.Main.Budgeting.Budgets;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Tenant.Budgeting.Enums;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>FAZ C — bütçe düzenleme komutlarının validator testleri.</summary>
public sealed class BudgetEditCommandValidatorTests
{
    private readonly UpdateBudgetLineVersionCommandValidator _updateLine = new(NullStringLocalizer.Instance);
    private readonly RemoveBudgetLineVersionCommandValidator _removeLine = new(NullStringLocalizer.Instance);
    private readonly UpdateBudgetCommandValidator _updateBudget = new(NullStringLocalizer.Instance);
    private readonly DeleteBudgetCommandValidator _deleteBudget = new(NullStringLocalizer.Instance);
    private readonly SetBudgetLineInstallmentsCommandValidator _setInst = new(NullStringLocalizer.Instance);

    private static UpdateBudgetLineVersionCommand ValidUpdateLine() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000m,
        PaymentSchedule.MonthlyEqual, DistributionModel.Equal, null, null, 15);

    [Fact]
    public void UpdateLine_dogru_basarili() => _updateLine.Validate(ValidUpdateLine()).IsValid.Should().BeTrue();

    [Fact]
    public void UpdateLine_negatif_tutar_hata()
        => _updateLine.Validate(ValidUpdateLine() with { PlannedAmount = -1m }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void UpdateLine_gecersiz_vade_gunu_hata(int day)
        => _updateLine.Validate(ValidUpdateLine() with { DueDayOfMonth = day }).IsValid.Should().BeFalse();

    [Fact]
    public void RemoveLine_bos_id_hata()
        => _removeLine.Validate(new RemoveBudgetLineVersionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UpdateBudget_dogru_basarili()
        => _updateBudget.Validate(new UpdateBudgetCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2026 Aidat"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void UpdateBudget_bos_baslik_hata()
        => _updateBudget.Validate(new UpdateBudgetCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteBudget_bos_id_hata()
        => _deleteBudget.Validate(new DeleteBudgetCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetInstallments_dogru_basarili()
        => _setInst.Validate(new SetBudgetLineInstallmentsCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            [new InstallmentInput(2026, 3, 5000m), new InstallmentInput(2026, 4, 5000m)])).IsValid.Should().BeTrue();

    [Fact]
    public void SetInstallments_gecersiz_ay_hata()
        => _setInst.Validate(new SetBudgetLineInstallmentsCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            [new InstallmentInput(2026, 13, 5000m)])).IsValid.Should().BeFalse();
}
