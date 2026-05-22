using CleanTenant.Application.Features.Main.Budgeting.Budgets;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Tenant.Budgeting.Enums;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>
/// <see cref="ReviseBudgetCommandValidator"/> testleri — LineOverrides için
/// nested validation kuralları dahil. v0.2.13.a FAZ 5 Slice 4e.
/// </summary>
public sealed class ReviseBudgetCommandValidatorTests
{
    private readonly ReviseBudgetCommandValidator _validator = new(NullStringLocalizer.Instance);

    private static ReviseBudgetCommand ValidCommand() => new(
        CompanyId: Guid.NewGuid(),
        BudgetId: Guid.NewGuid(),
        ValidFrom: new DateOnly(2026, 7, 1),
        Reason: "Elektrik fiyat artışı",
        LineOverrides: null);

    [Fact]
    public void Tum_alanlar_dogru_validation_basarili()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bos_Reason_hata_uretir()
    {
        var cmd = ValidCommand() with { Reason = string.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReviseBudgetCommand.Reason));
    }

    [Fact]
    public void Uzun_Reason_hata_uretir()
    {
        var cmd = ValidCommand() with { Reason = new string('R', 1001) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LineOverride_Negatif_PlannedAmount_hata_uretir()
    {
        var cmd = ValidCommand() with
        {
            LineOverrides = new List<BudgetLineOverride>
            {
                new(BudgetLineId: Guid.NewGuid(),
                    NewPlannedAmount: -100m,
                    NewPaymentSchedule: null,
                    NewDistributionModel: null,
                    NewParticipationGroupId: null,
                    NewDistributionConfig: null,
                    NewDueDayOfMonth: null)
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LineOverride_Gecersiz_DueDay_hata_uretir()
    {
        var cmd = ValidCommand() with
        {
            LineOverrides = new List<BudgetLineOverride>
            {
                new(BudgetLineId: Guid.NewGuid(),
                    NewPlannedAmount: null,
                    NewPaymentSchedule: null,
                    NewDistributionModel: null,
                    NewParticipationGroupId: null,
                    NewDistributionConfig: null,
                    NewDueDayOfMonth: 32) // > 31
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LineOverride_Bos_BudgetLineId_hata_uretir()
    {
        var cmd = ValidCommand() with
        {
            LineOverrides = new List<BudgetLineOverride>
            {
                new(BudgetLineId: Guid.Empty,
                    NewPlannedAmount: 100m,
                    NewPaymentSchedule: PaymentSchedule.MonthlyEqual,
                    NewDistributionModel: DistributionModel.Equal,
                    NewParticipationGroupId: null,
                    NewDistributionConfig: null,
                    NewDueDayOfMonth: 15)
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Bos_LineOverrides_listesi_validation_basarili()
    {
        var cmd = ValidCommand() with { LineOverrides = new List<BudgetLineOverride>() };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
