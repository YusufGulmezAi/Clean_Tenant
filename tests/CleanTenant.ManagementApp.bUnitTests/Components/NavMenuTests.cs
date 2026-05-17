using CleanTenant.ManagementApp.Components.Layout;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary><see cref="NavMenu"/> drawer içeriği testleri.</summary>
public sealed class NavMenuTests : MudTestContextBase
{
    [Fact]
    public void Dashboard_link_render_edilmeli()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("Dashboard");
    }

    [Fact]
    public void Yonetim_grup_basliklari_render_edilmeli()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("Yönetim");
        cut.Markup.Should().Contain("Gözetim");
        cut.Markup.Should().Contain("Ayarlar");
    }

    [Fact]
    public void Faz_1_disabled_linkler_chip_ile_isaretlenmeli()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("Tenant Yönetimi");
        cut.Markup.Should().Contain("Roller");
        cut.Markup.Should().Contain("Faz 1.4");
        cut.Markup.Should().Contain("Faz 1.5");
        cut.Markup.Should().Contain("Faz 1.6");
    }

    [Fact]
    public void Ayarlar_alt_linkler_render_edilmeli()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("Tema");
        cut.Markup.Should().Contain("Dil");
        cut.Markup.Should().Contain("Hakkında");
    }
}
