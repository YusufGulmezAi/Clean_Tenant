using CleanTenant.Application.Features.Main.LateFees.Calculation;
using CleanTenant.Domain.Tenant.LateFees;

namespace CleanTenant.Application.UnitTests.LateFees;

/// <summary>
/// <see cref="LateFeePolicyResolver"/> testleri — bütçe override → şirket varsayılanı.
/// FAZ 7B Slice 6.
/// </summary>
public sealed class LateFeePolicyResolverTests
{
    private readonly LateFeePolicyResolver _sut = new();

    private static LateFeePolicy Policy(Guid? budgetId, bool active = true, decimal rate = 3m) => new()
    {
        BudgetId = budgetId,
        IsActive = active,
        MonthlyRatePercent = rate,
    };

    [Fact]
    public void Butce_override_varsa_onu_doner()
    {
        var budgetId = Guid.NewGuid();
        var companyDefault = Policy(null, rate: 3m);
        var budgetOverride = Policy(budgetId, rate: 4m);

        var result = _sut.Resolve([companyDefault, budgetOverride], budgetId);

        result.Should().BeSameAs(budgetOverride);
    }

    [Fact]
    public void Butce_override_yoksa_sirket_varsayilanina_duser()
    {
        var companyDefault = Policy(null);

        var result = _sut.Resolve([companyDefault], Guid.NewGuid());

        result.Should().BeSameAs(companyDefault);
    }

    [Fact]
    public void Butce_override_pasifse_sirket_varsayilanina_duser()
    {
        var budgetId = Guid.NewGuid();
        var companyDefault = Policy(null);
        var inactiveOverride = Policy(budgetId, active: false);

        var result = _sut.Resolve([companyDefault, inactiveOverride], budgetId);

        result.Should().BeSameAs(companyDefault);
    }

    [Fact]
    public void BudgetId_null_ise_sirket_varsayilani_doner()
    {
        var budgetOverride = Policy(Guid.NewGuid());
        var companyDefault = Policy(null);

        var result = _sut.Resolve([budgetOverride, companyDefault], null);

        result.Should().BeSameAs(companyDefault);
    }

    [Fact]
    public void Hicbir_politika_yoksa_null()
    {
        _sut.Resolve([], Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void Yalniz_butce_override_var_default_yok_ve_budgetId_null_ise_null()
    {
        var budgetOverride = Policy(Guid.NewGuid());

        _sut.Resolve([budgetOverride], null).Should().BeNull();
    }
}
