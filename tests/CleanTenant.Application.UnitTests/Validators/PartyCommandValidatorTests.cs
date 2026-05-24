using CleanTenant.Application.Features.Main.Parties;
using CleanTenant.Application.Features.Main.Parties.Tenures;
using CleanTenant.Application.UnitTests.Common;
using CleanTenant.Domain.Tenant.Parties.Enums;

namespace CleanTenant.Application.UnitTests.Validators;

/// <summary>F0 S1 — Party + tenure komutlarının validator testleri.</summary>
public sealed class PartyCommandValidatorTests
{
    private readonly CreatePartyCommandValidator _createParty = new(NullStringLocalizer.Instance);
    private readonly AddUnitOwnershipCommandValidator _addOwner = new(NullStringLocalizer.Instance);
    private readonly AddUnitTenancyCommandValidator _addTenant = new(NullStringLocalizer.Instance);

    private static CreatePartyCommand ValidParty() => new(
        Guid.NewGuid(), Guid.NewGuid(), PartyKind.Individual, "Mehmet Eren Yılmaz");

    [Fact]
    public void CreateParty_dogru_basarili() => _createParty.Validate(ValidParty()).IsValid.Should().BeTrue();

    [Fact]
    public void CreateParty_bos_ad_hata()
        => _createParty.Validate(ValidParty() with { FullName = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void CreateParty_gecersiz_tckn_uzunluk_hata()
        => _createParty.Validate(ValidParty() with { Tckn = "123" }).IsValid.Should().BeFalse();

    private static AddUnitOwnershipCommand ValidOwner() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        new DateOnly(2026, 1, 1), null, 60m, true);

    [Fact]
    public void AddOwner_dogru_basarili() => _addOwner.Validate(ValidOwner()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(150)]
    public void AddOwner_gecersiz_pay_hata(decimal share)
        => _addOwner.Validate(ValidOwner() with { SharePercent = share }).IsValid.Should().BeFalse();

    [Fact]
    public void AddOwner_bitis_baslangictan_once_hata()
        => _addOwner.Validate(ValidOwner() with { EndDate = new DateOnly(2025, 12, 1) }).IsValid.Should().BeFalse();

    private static AddUnitTenancyCommand ValidTenant() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    [Fact]
    public void AddTenant_dogru_basarili() => _addTenant.Validate(ValidTenant()).IsValid.Should().BeTrue();

    [Fact]
    public void AddTenant_bitis_baslangictan_once_hata()
        => _addTenant.Validate(ValidTenant() with { EndDate = new DateOnly(2025, 1, 1) }).IsValid.Should().BeFalse();
}
