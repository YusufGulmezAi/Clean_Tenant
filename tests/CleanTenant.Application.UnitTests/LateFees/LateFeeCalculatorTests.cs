using CleanTenant.Application.Features.Main.LateFees.Calculation;

namespace CleanTenant.Application.UnitTests.LateFees;

/// <summary>
/// <see cref="LateFeeCalculator"/> testleri — basit faiz, KMK m.20 tavanı, grace.
/// FAZ 7B Slice 6.
/// </summary>
public sealed class LateFeeCalculatorTests
{
    private readonly LateFeeCalculator _sut = new();

    [Fact]
    public void Vadesi_30_gun_gecmis_borc_aylik_oranla_hesaplanir()
    {
        // SDD FR-19 kabul: 1000 TL, 30 gün, aylık %3 → 30 TL
        var fee = _sut.ComputeForDebt(
            remainingPrincipal: 1000m,
            dueDate: new DateOnly(2026, 1, 1),
            graceDays: 0,
            monthlyRatePercent: 3m,
            asOfDate: new DateOnly(2026, 1, 31));

        fee.Should().Be(30m);
    }

    [Fact]
    public void Oran_KMK_tavanini_asarsa_tavan_uygulanir()
    {
        // Oran %10 ama KMK m.20 tavanı %5 → 1000 × %5 × 30/30 = 50
        var fee = _sut.ComputeForDebt(1000m, new DateOnly(2026, 1, 1), 0, 10m, new DateOnly(2026, 1, 31));
        fee.Should().Be(50m);
    }

    [Fact]
    public void Vade_henuz_gecmemisse_sifir()
    {
        var fee = _sut.ComputeForDebt(1000m, new DateOnly(2026, 2, 1), 0, 3m, new DateOnly(2026, 1, 15));
        fee.Should().Be(0m);
    }

    [Fact]
    public void Vade_gunu_ile_esit_tarihte_sifir()
    {
        var fee = _sut.ComputeForDebt(1000m, new DateOnly(2026, 1, 1), 0, 3m, new DateOnly(2026, 1, 1));
        fee.Should().Be(0m);
    }

    [Fact]
    public void Grace_suresi_icinde_sifir_sonrasinda_hesaplanir()
    {
        var due = new DateOnly(2026, 1, 1);

        // Grace 10 gün; 10. gün içinde → 0
        _sut.ComputeForDebt(1000m, due, 10, 3m, new DateOnly(2026, 1, 11)).Should().Be(0m);

        // Grace sonrası 10 gün (01-11 → 01-21) → 1000 × %3 × 10/30 ≈ 10
        _sut.ComputeForDebt(1000m, due, 10, 3m, new DateOnly(2026, 1, 21))
            .Should().BeApproximately(10m, 0.001m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Pozitif_olmayan_anapara_sifir(decimal principal)
    {
        var fee = _sut.ComputeForDebt(principal, new DateOnly(2026, 1, 1), 0, 3m, new DateOnly(2026, 6, 1));
        fee.Should().Be(0m);
    }
}
