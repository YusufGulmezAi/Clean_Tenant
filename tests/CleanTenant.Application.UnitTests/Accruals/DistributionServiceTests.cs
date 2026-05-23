using CleanTenant.Application.Features.Main.Accruals.Distribution;
using CleanTenant.Domain.Tenant.Budgeting.Enums;

namespace CleanTenant.Application.UnitTests.Accruals;

/// <summary>
/// <see cref="DistributionService"/> testleri — LRM yuvarlama (kuruş kaybı yok),
/// eşit + m² dağılımı. FAZ 6 Slice 4.
/// </summary>
public sealed class DistributionServiceTests
{
    private readonly DistributionService _sut = new();

    private static DistributionUnit U(decimal m2) => new(Guid.NewGuid(), m2);

    [Fact]
    public void Equal_3BB_1000TL_kurus_kaybi_olmadan_dagitir()
    {
        var units = new[] { U(0), U(0), U(0) };
        var result = _sut.Distribute(DistributionModel.Equal, 1000m, units);

        result.Sum(r => r.Amount).Should().Be(1000m);
        // 333.34 + 333.33 + 333.33
        result.Select(r => r.Amount).OrderByDescending(x => x).First().Should().Be(333.34m);
        result.Count(r => r.Amount == 333.33m).Should().Be(2);
    }

    [Fact]
    public void Equal_200BB_300000TL_tam_bolunur()
    {
        var units = Enumerable.Range(0, 200).Select(_ => U(0)).ToList();
        var result = _sut.Distribute(DistributionModel.Equal, 300_000m, units);

        result.Sum(r => r.Amount).Should().Be(300_000m);
        result.Should().OnlyContain(r => r.Amount == 1500m);
    }

    [Fact]
    public void BySquareMeter_oranli_dagitir_ve_toplam_korunur()
    {
        // 3 BB: 100, 60, 40 m² (toplam 200) — 1000 TL
        var u1 = U(100); var u2 = U(60); var u3 = U(40);
        var result = _sut.Distribute(DistributionModel.BySquareMeter, 1000m, new[] { u1, u2, u3 });

        result.Sum(r => r.Amount).Should().Be(1000m);
        result.Single(r => r.UnitId == u1.UnitId).Amount.Should().Be(500m);  // 100/200
        result.Single(r => r.UnitId == u2.UnitId).Amount.Should().Be(300m);  // 60/200
        result.Single(r => r.UnitId == u3.UnitId).Amount.Should().Be(200m);  // 40/200
    }

    [Fact]
    public void BySquareMeter_kusuratli_LRM_ile_toplam_korunur()
    {
        // 3 eşit m² ama tutar tam bölünmüyor → 1000 / 3
        var result = _sut.Distribute(DistributionModel.BySquareMeter, 1000m,
            new[] { U(10), U(10), U(10) });

        result.Sum(r => r.Amount).Should().Be(1000m);
    }

    [Fact]
    public void BySquareMeter_tum_m2_sifirsa_esit_dagitima_duser()
    {
        var result = _sut.Distribute(DistributionModel.BySquareMeter, 900m,
            new[] { U(0), U(0), U(0) });

        result.Sum(r => r.Amount).Should().Be(900m);
        result.Should().OnlyContain(r => r.Amount == 300m);
    }

    [Fact]
    public void Bos_BB_listesi_bos_sonuc_doner()
    {
        var result = _sut.Distribute(DistributionModel.Equal, 1000m, []);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Desteklenmeyen_model_NotSupported_firlatir()
    {
        var act = () => _sut.Distribute(DistributionModel.Formula, 100m, new[] { U(10) });
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Kurusatli_buyuk_senaryo_toplam_her_zaman_korunur()
    {
        // 7 BB farklı m², 12.345,67 TL — LRM toplamı korumalı
        var units = new[] { U(123), U(87), U(56), U(200), U(45), U(99), U(150) };
        var result = _sut.Distribute(DistributionModel.BySquareMeter, 12_345.67m, units);

        result.Sum(r => r.Amount).Should().Be(12_345.67m);
    }
}
