using CleanTenant.ManagementApp.Components.Pages.Settings;
using CleanTenant.ManagementApp.Themes;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary><see cref="ThemeSettings"/> 4 preset render + seçim testleri.</summary>
public sealed class ThemeSettingsTests : MudTestContextBase
{
    [Fact]
    public void Dort_preset_karti_render_edilmeli()
    {
        var cut = RenderComponent<ThemeSettings>();

        cut.Markup.Should().Contain("Kurumsal Mavi");
        cut.Markup.Should().Contain("Teşkilatsal Yeşil");
        cut.Markup.Should().Contain("MudBlazor Mor");
        cut.Markup.Should().Contain("Koyu Kurumsal");
    }

    [Fact]
    public void Sayfa_basligi_ve_aciklama_render_edilmeli()
    {
        var cut = RenderComponent<ThemeSettings>();

        cut.Markup.Should().Contain("Tema Ayarları");
        cut.Markup.Should().Contain("4 hazır tema preset");
    }

    [Fact]
    public void Dark_mode_switch_render_edilmeli()
    {
        var cut = RenderComponent<ThemeSettings>();

        cut.Markup.Should().MatchRegex("Açık mod aktif|Koyu mod aktif");
    }
}
