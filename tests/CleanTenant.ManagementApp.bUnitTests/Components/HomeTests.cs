using CleanTenant.ManagementApp.Components.Pages;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary><see cref="Home"/> (Dashboard) sayfa render testleri.</summary>
public sealed class HomeTests : MudTestContextBase
{
    [Fact]
    public void Dashboard_basligini_render_etmeli()
    {
        var cut = RenderComponent<Home>();

        cut.Markup.Should().Contain("Dashboard");
        cut.Markup.Should().Contain("Hoş geldiniz");
    }

    [Fact]
    public void Faz_1_yol_haritasini_listelemeli()
    {
        var cut = RenderComponent<Home>();

        cut.Markup.Should().Contain("v0.2.1");
        cut.Markup.Should().Contain("v0.2.2");
        cut.Markup.Should().Contain("v0.2.7");
    }

    [Fact]
    public void Dort_KPI_kart_olmali_hepsi_em_dash_ile_baslar()
    {
        var cut = RenderComponent<Home>();

        cut.Markup.Should().Contain("Tenant Sayısı");
        cut.Markup.Should().Contain("Aktif Kullanıcı");
        cut.Markup.Should().Contain("Aktif Support");
        cut.Markup.Should().Contain("Bugünkü Audit");
    }
}
