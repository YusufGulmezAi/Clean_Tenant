using CleanTenant.ManagementApp.Themes;

namespace CleanTenant.ManagementApp.bUnitTests.Themes;

/// <summary>
/// <see cref="CleanTenantThemes"/> resolve + isimlendirme testleri. Tema
/// preset'lerinin runtime'da doğru çözüldüğünü ve UI metinlerinin Türkçe
/// görünür adlarla geldiğini doğrular.
/// </summary>
public sealed class CleanTenantThemesTests
{
    [Theory]
    [InlineData(ThemePresetId.KurumsalMavi)]
    [InlineData(ThemePresetId.TeskilatsalYesil)]
    [InlineData(ThemePresetId.MudBlazorMor)]
    [InlineData(ThemePresetId.KoyuKurumsal)]
    public void Resolve_4_preset_icin_non_null_tema_donmeli(ThemePresetId preset)
    {
        var theme = CleanTenantThemes.Resolve(preset);

        theme.Should().NotBeNull();
        theme.PaletteLight.Should().NotBeNull();
        theme.PaletteDark.Should().NotBeNull();
        theme.PaletteLight.Primary.Value.Should().NotBeNullOrWhiteSpace();
        theme.PaletteDark.Primary.Value.Should().NotBeNullOrWhiteSpace();
    }

    // Not: MudBlazor renkleri 8-haneli RGBA olarak parse eder (#1976D2 → #1976d2ff).
    // İlk 7 hex karakter karşılaştırılır (case-insensitive).

    [Fact]
    public void Resolve_KurumsalMavi_primary_1976D2_olmali()
    {
        var theme = CleanTenantThemes.Resolve(ThemePresetId.KurumsalMavi);

        theme.PaletteLight.Primary.Value.ToLowerInvariant().Should().StartWith("#1976d2");
    }

    [Fact]
    public void Resolve_TeskilatsalYesil_primary_2E7D32_olmali()
    {
        var theme = CleanTenantThemes.Resolve(ThemePresetId.TeskilatsalYesil);

        theme.PaletteLight.Primary.Value.ToLowerInvariant().Should().StartWith("#2e7d32");
    }

    [Fact]
    public void Resolve_KoyuKurumsal_primary_263238_olmali()
    {
        var theme = CleanTenantThemes.Resolve(ThemePresetId.KoyuKurumsal);

        theme.PaletteLight.Primary.Value.ToLowerInvariant().Should().StartWith("#263238");
    }

    [Theory]
    [InlineData(ThemePresetId.KurumsalMavi, "Kurumsal Mavi")]
    [InlineData(ThemePresetId.TeskilatsalYesil, "Teşkilatsal Yeşil")]
    [InlineData(ThemePresetId.MudBlazorMor, "MudBlazor Mor")]
    [InlineData(ThemePresetId.KoyuKurumsal, "Koyu Kurumsal")]
    public void DisplayName_Türkçe_görünür_ad_dönmeli(ThemePresetId preset, string expected)
    {
        var name = CleanTenantThemes.DisplayName(preset);

        name.Should().Be(expected);
    }

    [Fact]
    public void Resolve_bilinmeyen_preset_KurumsalMavi_default_donmeli()
    {
        var theme = CleanTenantThemes.Resolve((ThemePresetId)999);

        theme.Should().BeSameAs(CleanTenantThemes.KurumsalMavi);
    }
}
