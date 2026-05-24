using CleanTenant.Application.Features.Main.Parties.Responsibility;
using CleanTenant.Domain.Tenant.Parties.Enums;
using OW = CleanTenant.Application.Features.Main.Parties.Responsibility.ResponsibilityProrator.OwnerWindow;
using TW = CleanTenant.Application.Features.Main.Parties.Responsibility.ResponsibilityProrator.TenantWindow;

namespace CleanTenant.Application.UnitTests.Responsibility;

/// <summary>
/// F0 S2 — gün-bazlı proration motoru testleri. Örnek dönem: Mayıs 2026 (31 gün).
/// </summary>
public sealed class ResponsibilityProratorTests
{
    private static readonly Guid Owner1 = Guid.NewGuid();
    private static readonly Guid Owner2 = Guid.NewGuid();
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    private static OW Own(Guid id, int sm, int sd, int? em = null, int? ed = null, decimal share = 100m)
        => new(id, new DateOnly(2026, sm, sd), em.HasValue ? new DateOnly(2026, em.Value, ed!.Value) : null, share);

    private static TW Ten(Guid id, int sm, int sd, int? em = null, int? ed = null)
        => new(id, new DateOnly(2026, sm, sd), em.HasValue ? new DateOnly(2026, em.Value, ed!.Value) : null);

    [Fact]
    public void Tek_malik_tum_donem_yuzde_yuz()
    {
        var r = ResponsibilityProrator.Prorate(2026, 5, 3100m, ResponsibilityMode.OwnerOnly,
            [Own(Owner1, 1, 1)], []);

        r.PrimaryPartyId.Should().Be(Owner1);
        r.Splits.Should().ContainSingle();
        r.Splits[0].Amount.Should().Be(3100m);
        r.Splits[0].DayCount.Should().Be(31);
        r.Splits[0].Kind.Should().Be(ResponsibilityKind.Owner);
    }

    [Fact]
    public void TenantThenOwner_tek_kiraci_kiraciya_yansir()
    {
        var r = ResponsibilityProrator.Prorate(2026, 5, 3100m, ResponsibilityMode.TenantThenOwner,
            [Own(Owner1, 1, 1)], [Ten(TenantA, 1, 1)]);

        r.PrimaryPartyId.Should().Be(TenantA);
        r.Splits.Should().ContainSingle();
        r.Splits[0].Kind.Should().Be(ResponsibilityKind.Tenant);
        r.Splits[0].Amount.Should().Be(3100m);
    }

    [Fact]
    public void OwnerOnly_kiraci_yok_sayilir()
    {
        var r = ResponsibilityProrator.Prorate(2026, 5, 3100m, ResponsibilityMode.OwnerOnly,
            [Own(Owner1, 1, 1)], [Ten(TenantA, 1, 1)]);

        r.PrimaryPartyId.Should().Be(Owner1);
        r.Splits.Should().ContainSingle();
        r.Splits[0].Kind.Should().Be(ResponsibilityKind.Owner);
    }

    [Fact]
    public void Mid_month_kiraci_cikis_bos_donem_yeni_kiraci_uc_parca()
    {
        // Kiracı A 1–12 (12g), boş 13–19 → malik (7g), Kiracı B 20–31 (12g). 31g, günlük 100.
        var r = ResponsibilityProrator.Prorate(2026, 5, 3100m, ResponsibilityMode.TenantThenOwner,
            [Own(Owner1, 1, 1)],
            [Ten(TenantA, 1, 1, 5, 12), Ten(TenantB, 5, 20)]);

        r.Splits.Should().HaveCount(3);
        r.Splits.Sum(s => s.Amount).Should().Be(3100m);
        r.Splits.Sum(s => s.DayCount).Should().Be(31);

        var a = r.Splits.Single(s => s.PartyId == TenantA);
        var o = r.Splits.Single(s => s.PartyId == Owner1);
        var b = r.Splits.Single(s => s.PartyId == TenantB);
        a.DayCount.Should().Be(12); a.Amount.Should().Be(1200m); a.Kind.Should().Be(ResponsibilityKind.Tenant);
        o.DayCount.Should().Be(7);  o.Amount.Should().Be(700m);  o.Kind.Should().Be(ResponsibilityKind.Owner);
        b.DayCount.Should().Be(12); b.Amount.Should().Be(1200m); b.Kind.Should().Be(ResponsibilityKind.Tenant);
        // Birincil = en çok günlü (A ve B 12'şer eşit → ilki); en azından bir kiracı
        r.PrimaryPartyId.Should().NotBeNull();
        new[] { TenantA, TenantB }.Should().Contain(r.PrimaryPartyId!.Value);
    }

    [Fact]
    public void Co_owner_birincil_en_yuksek_payli_malik()
    {
        // İki malik aynı dönem; %60 ve %40 → birincil %60'lı. Borç tek (bölünmez).
        var r = ResponsibilityProrator.Prorate(2026, 5, 3100m, ResponsibilityMode.OwnerOnly,
            [Own(Owner1, 1, 1, share: 60m), Own(Owner2, 1, 1, share: 40m)], []);

        r.PrimaryPartyId.Should().Be(Owner1);
        r.Splits.Should().ContainSingle();
        r.Splits[0].PartyId.Should().Be(Owner1);
        r.Splits[0].Amount.Should().Be(3100m);
    }

    [Fact]
    public void Tenure_yok_sorumlu_atanamaz()
    {
        var r = ResponsibilityProrator.Prorate(2026, 5, 3100m, ResponsibilityMode.TenantThenOwner, [], []);
        r.PrimaryPartyId.Should().BeNull();
        r.Splits.Should().BeEmpty();
    }

    [Fact]
    public void Kurus_artigi_toplami_korur()
    {
        // 100 TL / 2 eşit parça olmayan bölüm: 1000.01 / Şubat 28 gün, ortada değişim
        var r = ResponsibilityProrator.Prorate(2026, 2, 1000.01m, ResponsibilityMode.TenantThenOwner,
            [Own(Owner1, 1, 1)], [Ten(TenantA, 1, 1, 2, 14)]);

        r.Splits.Sum(s => s.Amount).Should().Be(1000.01m); // kuruş artığı kaybolmaz
        r.Splits.Should().HaveCount(2);
    }
}
